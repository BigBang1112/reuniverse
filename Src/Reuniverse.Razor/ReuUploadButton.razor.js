const pasteHandlers = new WeakMap();

export function addHandlers(element, dotNetHelper, streamed) {
    element.addEventListener("change", async (event) => {
        if (event.target.files.length === 0) {
            return;
        }

        // a picked folder brings every file in it, as the picker can't filter them by accept
        const files = element.webkitdirectory
            ? filterAccepted(event.target.files, element.getAttribute("accept"))
            : Array.from(event.target.files);

        await handleFiles(files, dotNetHelper, streamed);

        event.target.value = null;
    });

    // pasting is turned on and off by the data-reu-paste attribute, so the component can change it at any time
    const paste = async (event) => {
        if (!element.isConnected || !element.hasAttribute("data-reu-paste") || element.closest(":disabled") || isTextEntry(event.target)) {
            return;
        }

        const pasted = event.clipboardData ? Array.from(event.clipboardData.files) : [];

        if (pasted.length === 0) {
            return;
        }

        event.preventDefault();

        await handleFiles(filterAccepted(pasted, element.getAttribute("accept")), dotNetHelper, streamed);
    };

    document.addEventListener("paste", paste);
    pasteHandlers.set(element, paste);
}

export function removeHandlers(element) {
    const paste = pasteHandlers.get(element);

    if (paste) {
        document.removeEventListener("paste", paste);
        pasteHandlers.delete(element);
    }
}

async function handleFiles(files, dotNetHelper, streamed) {
    if (files.length === 0) {
        return;
    }

    const maxFileCount = await dotNetHelper.invokeMethodAsync("GetMaxFileCountAsync");
    const maxFileSize = await dotNetHelper.invokeMethodAsync("GetMaxFileSizeAsync");

    const toUploadCount = maxFileCount == 0 ? files.length : Math.min(files.length, maxFileCount);

    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        // only files from a picked or dropped folder have a path
        const relativePath = file.webkitRelativePath || null;
        if (i >= toUploadCount) {
            await dotNetHelper.invokeMethodAsync("OnFileExceedCountAsync", file.name, file.size, file.lastModified, file.type, relativePath);
            continue;
        }
        if (maxFileSize > 0 && file.size > maxFileSize) {
            await dotNetHelper.invokeMethodAsync("OnFileTooLargeAsync", file.name, file.size, file.lastModified, file.type, relativePath);
            continue;
        }
        if (streamed) {
            await dotNetHelper.invokeMethodAsync("OpenStreamAsync", createFileReference(file), file.name, file.size, file.lastModified, file.type, relativePath);
            continue;
        }
        const data = new Uint8Array(await file.arrayBuffer());
        await dotNetHelper.invokeMethodAsync("UploadAsync", file.name, data, file.lastModified, file.type, relativePath);
    }
}

// Keeps the files that the accept attribute allows, like the file picker does: by extension, exact type or type/*.
function filterAccepted(files, accept) {
    const tokens = (accept || "").split(",").map(token => token.trim().toLowerCase()).filter(Boolean);

    if (tokens.length === 0) {
        return Array.from(files);
    }

    return Array.from(files).filter(file => {
        const name = file.name.toLowerCase();
        const type = (file.type || "").toLowerCase();

        return tokens.some(token => token.startsWith(".")
            ? name.endsWith(token)
            : token.endsWith("/*") ? type.startsWith(token.slice(0, -1)) : type === token);
    });
}

// Pasting into a text field is left to the field.
function isTextEntry(target) {
    if (!(target instanceof Element)) {
        return false;
    }

    if (target instanceof HTMLTextAreaElement || target.isContentEditable) {
        return true;
    }

    return target instanceof HTMLInputElement
        && !["file", "checkbox", "radio", "button", "submit", "reset", "range", "color", "image"].includes(target.type);
}

// .NET reads the file in ranges through this object, so the file is never loaded whole.
// read returns bytes (WebAssembly), slice returns a Blob that .NET receives as a stream (server, where messages are size-limited).
function createFileReference(file) {
    return DotNet.createJSObjectReference({
        read: async (offset, count) => new Uint8Array(await file.slice(offset, offset + count).arrayBuffer()),
        slice: (offset, count) => file.slice(offset, offset + count)
    });
}
