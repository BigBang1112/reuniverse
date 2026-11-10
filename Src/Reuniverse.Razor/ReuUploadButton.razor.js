export function addHandlers(element, dotNetHelper, streamed) {
    element.addEventListener("change", async (event) => {
        if (event.target.files.length === 0) {
            return;
        }

        const maxFileCount = await dotNetHelper.invokeMethodAsync("GetMaxFileCountAsync");
        const maxFileSize = await dotNetHelper.invokeMethodAsync("GetMaxFileSizeAsync");

        const files = event.target.files;
        const toUploadCount = maxFileCount == 0 ? files.length : Math.min(files.length, maxFileCount);

        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            if (i >= toUploadCount) {
                await dotNetHelper.invokeMethodAsync("OnFileExceedCountAsync", file.name, file.size, file.lastModified, file.type);
                continue;
            }
            if (maxFileSize > 0 && file.size > maxFileSize) {
                await dotNetHelper.invokeMethodAsync("OnFileTooLargeAsync", file.name, file.size, file.lastModified, file.type);
                continue;
            }
            if (streamed) {
                await dotNetHelper.invokeMethodAsync("OpenStreamAsync", createFileReference(file), file.name, file.size, file.lastModified, file.type);
                continue;
            }
            const data = new Uint8Array(await file.arrayBuffer());
            await dotNetHelper.invokeMethodAsync("UploadAsync", file.name, data, file.lastModified, file.type);
        }

        event.target.value = null;
    });
}

// .NET reads the file in ranges through this object, so the file is never loaded whole.
// read returns bytes (WebAssembly), slice returns a Blob that .NET receives as a stream (server, where messages are size-limited).
function createFileReference(file) {
    return DotNet.createJSObjectReference({
        read: async (offset, count) => new Uint8Array(await file.slice(offset, offset + count).arrayBuffer()),
        slice: (offset, count) => file.slice(offset, offset + count)
    });
}
