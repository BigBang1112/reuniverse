let dotNetHelper = null;
let dragCounter = 0;

export function addHandlers(helper) {
    dotNetHelper = helper;
    dragCounter = 0;

    // Add document-level drag event listeners
    document.addEventListener('dragenter', handleDragEnter, true);
    document.addEventListener('dragleave', handleDragLeave, true);
    document.addEventListener('dragover', handleDragOver, true);
    document.addEventListener('drop', handleDrop, true);
}

function handleDragEnter(e) {
    //e.preventDefault();
    //e.stopPropagation();
    
    // Only handle file drags
    if (hasFiles(e)) {
        dragCounter++;
        if (dragCounter === 1 && dotNetHelper) {
            // Show the upload screen
            dotNetHelper.invokeMethodAsync('SetVisible', true);
        }
    }
}

function handleDragLeave(e) {
    //e.preventDefault();
    //e.stopPropagation();
    
    if (hasFiles(e)) {
        dragCounter--;
        if (dragCounter <= 0 && dotNetHelper) {
            dragCounter = 0;
            // Hide the upload screen
            dotNetHelper.invokeMethodAsync('SetVisible', false);
        }
    }
}

function handleDragOver(e) {
    //e.preventDefault();
    //e.stopPropagation();
    
    if (hasFiles(e)) {
        e.dataTransfer.dropEffect = 'copy';
    }
}

function handleDrop(e) {
    dragCounter = 0;
}

function hasFiles(e) {
    if (!e.dataTransfer || !e.dataTransfer.types) {
        return false;
    }
    
    // Check if the drag contains files
    return e.dataTransfer.types.includes('Files');
}

export function removeHandlers() {
    if (dotNetHelper) {
        document.removeEventListener('dragenter', handleDragEnter, true);
        document.removeEventListener('dragleave', handleDragLeave, true);
        document.removeEventListener('dragover', handleDragOver, true);
        document.removeEventListener('drop', handleDrop, true);

        dotNetHelper = null;
        dragCounter = 0;
    }
}