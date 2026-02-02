export function addHandlers(element, dotNetHelper, streamed) {
    element.addEventListener("change", async (event) => {
        if (event.target.files.length === 0) {
            return;
        }

        if (!streamed) {
            const maxFileCount = await dotNetHelper.invokeMethodAsync("GetMaxFileCountAsync");
            const maxFileSize = await dotNetHelper.invokeMethodAsync("GetMaxFileSizeAsync");

            const files = event.target.files;
            const toUploadCount = maxFileCount == 0 ? files.length : Math.min(files.length, maxFileCount);

            for (let i = 0; i < files.length; i++) {
                const file = files[i];
                if (i >= toUploadCount) {
                    await dotNetHelper.invokeMethodAsync("OnFileExceedCountAsync", file.name, file.size, file.lastModified);
                    continue;
                }
                if (maxFileSize > 0 && file.size > maxFileSize) {
                    await dotNetHelper.invokeMethodAsync("OnFileTooLargeAsync", file.name, file.size, file.lastModified);
                    continue;
                }
                const data = new Uint8Array(await file.arrayBuffer());
                await dotNetHelper.invokeMethodAsync("UploadAsync", file.name, data, file.lastModified);
            }
        }

        event.target.value = null;
    });
}