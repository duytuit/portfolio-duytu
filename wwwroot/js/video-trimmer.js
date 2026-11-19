(() => {
    const uploadForm = document.querySelector('.video-tool__upload');
    if (uploadForm) {
        const fileInput = uploadForm.querySelector('#video-file');
        const dropzone = uploadForm.querySelector('[data-video-dropzone]');
        const fileName = uploadForm.querySelector('[data-video-file-name]');
        const fileHint = uploadForm.querySelector('[data-video-file-hint]');
        const cutButton = uploadForm.querySelector('[data-cut-button]');
        const formatBytes = (bytes) => bytes < 1024 * 1024
            ? `${Math.max(1, Math.round(bytes / 1024))} KB`
            : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
        const showFile = () => {
            const file = fileInput.files && fileInput.files[0];
            if (!file) {
                fileName.textContent = 'Chọn video để bắt đầu';
                fileHint.textContent = 'Kéo tệp vào đây hoặc nhấn để duyệt';
                dropzone.dataset.hasFile = 'false';
                return;
            }
            fileName.textContent = file.name;
            fileHint.textContent = `${formatBytes(file.size)} · Sẵn sàng để cắt`;
            dropzone.dataset.hasFile = 'true';
        };
        fileInput.addEventListener('change', showFile);
        ['dragenter', 'dragover'].forEach(eventName => dropzone.addEventListener(eventName, event => {
            event.preventDefault();
            dropzone.classList.add('is-dragging');
        }));
        const stopDragging = () => dropzone.classList.remove('is-dragging');
        dropzone.addEventListener('dragleave', event => {
            if (!dropzone.contains(event.relatedTarget)) stopDragging();
        });
        dropzone.addEventListener('dragend', stopDragging);
        dropzone.addEventListener('drop', event => {
            event.preventDefault();
            dropzone.classList.remove('is-dragging');
            const files = event.dataTransfer && event.dataTransfer.files;
            if (!files || files.length === 0) return;
            try {
                const transfer = new DataTransfer();
                transfer.items.add(files[0]);
                fileInput.files = transfer.files;
                fileInput.dispatchEvent(new Event('change', { bubbles: true }));
            } catch {
                fileHint.textContent = 'Không thể nhận tệp này. Nhấn “Chọn tệp” để duyệt.';
            }
        });
        uploadForm.addEventListener('submit', () => {
            if (!fileInput.files || fileInput.files.length === 0) return;
            cutButton.disabled = true;
            cutButton.querySelector('span').textContent = 'Đang xử lý…';
        });
    }

    const form = document.querySelector('[data-video-delete-form]');
    if (!form) return;

    const selectAll = form.querySelector('[data-select-all]');
    const clipCheckboxes = Array.from(form.querySelectorAll('.video-tool__clip-checkbox'));
    const deleteSelected = form.querySelector('[data-delete-selected]');
    const updateSelection = () => {
        const selectedCount = clipCheckboxes.filter(checkbox => checkbox.checked).length;
        deleteSelected.disabled = selectedCount === 0;
        selectAll.checked = clipCheckboxes.length > 0 && selectedCount === clipCheckboxes.length;
        selectAll.indeterminate = selectedCount > 0 && selectedCount < clipCheckboxes.length;
    };

    updateSelection();
    selectAll.addEventListener('change', () => {
        clipCheckboxes.forEach(checkbox => { checkbox.checked = selectAll.checked; });
        updateSelection();
    });
    clipCheckboxes.forEach(checkbox => checkbox.addEventListener('change', updateSelection));

    form.addEventListener('submit', event => {
        if (!window.confirm('Xóa tất cả video đã chọn?')) event.preventDefault();
    });
})();
