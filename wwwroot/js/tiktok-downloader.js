(() => {
    const form = document.querySelector('[data-tiktok-form]');
    if (!form) return;
    const urlInput = form.querySelector('[name="url"]');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const preview = document.querySelector('[data-preview]');
    const progress = document.querySelector('[data-progress]');
    const history = document.querySelector('[data-history]');
    let currentId = null;
    let timer = null;
    let infoReady = false;
    const region = { x: 0, y: 0, width: 0, height: 0 };
    const stage = preview.querySelector('[data-stage]');
    const image = preview.querySelector('[data-thumbnail]');
    const selection = preview.querySelector('[data-selection]');
    const aiToggle = form.querySelector('[data-ai-toggle]');
    const regionHint = preview.querySelector('[data-region-hint]');
    const clearRegion = preview.querySelector('[data-clear-region]');
    let dragStart = null;
    let dragPointerId = null;
    const defaultRegionHint = 'Kéo trên ảnh để chọn vùng cần AI tái tạo.';

    const resetRegion = () => {
        Object.assign(region, { x: 0, y: 0, width: 0, height: 0 });
        dragStart = null;
        dragPointerId = null;
        selection.hidden = true;
        clearRegion.hidden = true;
        regionHint.textContent = defaultRegionHint;
    };
    urlInput.addEventListener('input', () => { infoReady = false; preview.hidden = true; resetRegion(); });
    aiToggle.addEventListener('change', () => {
        regionHint.hidden = !aiToggle.checked;
        preview.querySelector('[data-download]').textContent = aiToggle.checked ? 'Tải và xử lý AI' : 'Tạo tác vụ tải';
        if (!aiToggle.checked) resetRegion();
    });
    clearRegion.addEventListener('click', resetRegion);

    const post = async (path) => {
        const body = new URLSearchParams({
            __RequestVerificationToken: token,
            url: urlInput.value.trim(),
            rights: String(form.querySelector('[name="rights"]').checked),
            removeWatermark: String(aiToggle.checked),
            regionX: String(region.x),
            regionY: String(region.y),
            regionWidth: String(region.width),
            regionHeight: String(region.height)
        });
        const response = await fetch(path, { method: 'POST', body, headers: { RequestVerificationToken: token } });
        const data = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(data.error || `Máy chủ từ chối yêu cầu (HTTP ${response.status}).`);
        return data;
    };
    const showError = (error) => {
        const target = progress.hidden ? form : progress;
        const node = target.querySelector('[data-error]');
        if (node) node.textContent = error.message;
        else {
            form.querySelector('[data-form-error]').textContent = error.message;
        }
    };
    const addHistory = (job) => {
        const row = document.createElement('div');
        row.className = 'tiktok-tool__history-item';
        const date = document.createElement('span');
        date.textContent = new Date(job.createdUtc).toLocaleString();
        const status = document.createElement('strong');
        status.textContent = ({ Queued: 'Đang chờ', Downloading: 'Đang tải video', GeneratingMasks: 'Đang tạo mask', WaitingForAI: 'Đang chờ lượt AI', AIProcessing: 'ProPainter đang xử lý', Encoding: 'Đang tạo MP4', Completed: 'Hoàn tất', Failed: 'Thất bại', Cancelled: 'Đã hủy' })[job.status] || job.status;
        row.append(date, status);
        if (job.downloadUrl) {
            const link = document.createElement('a');
            link.href = job.downloadUrl;
            link.textContent = 'Tải MP4';
            row.append(link);
        }
        history.prepend(row);
    };
    const updateJob = (job) => {
        progress.hidden = false;
        progress.querySelector('[data-status]').textContent = job.status;
        progress.querySelector('[data-percent]').textContent = job.status === 'Downloading' ? `${Math.round(job.progress)}%` : '';
        const visibleProgress = job.status === 'Downloading' ? job.progress : job.status === 'Completed' ? 100 : 0;
        progress.querySelector('[data-bar]').style.width = `${Math.max(0, Math.min(100, visibleProgress))}%`;
        progress.querySelector('[data-error]').textContent = job.error || '';
        const result = progress.querySelector('[data-result]');
        result.hidden = !job.downloadUrl;
        if (job.downloadUrl) result.href = job.downloadUrl;
        const outputVideo = progress.querySelector('[data-output-video]');
        outputVideo.hidden = !job.downloadUrl;
        if (job.downloadUrl) outputVideo.src = job.downloadUrl;
        const cancel = progress.querySelector('[data-cancel]');
        cancel.hidden = ['Completed', 'Failed', 'Cancelled'].includes(job.status);
        if (cancel.hidden && timer) { clearInterval(timer); timer = null; }
    };

    form.addEventListener('submit', async (event) => {
        event.preventDefault();
        const button = form.querySelector('[data-info]');
        button.disabled = true;
        try {
            const data = await post('/tiktok/info');
            form.querySelector('[data-form-error]').textContent = '';
            preview.hidden = false;
            preview.querySelector('[data-title]').textContent = data.title;
            const duration = data.durationSeconds ? `${Math.round(data.durationSeconds)} giây` : 'Không rõ thời lượng';
            preview.querySelector('[data-meta]').textContent = [duration, data.resolution].filter(Boolean).join(' · ');
            resetRegion();
            try {
                const imageUrl = new URL(data.thumbnail);
                const approvedHosts = ['tiktokcdn.com', 'tiktokcdn-us.com', 'tiktokcdn-com.akamaized.net', 'byteoversea.net', 'ibytedtos.com'];
                const approved = imageUrl.protocol === 'https:' && approvedHosts.some(host => imageUrl.hostname === host || imageUrl.hostname.endsWith(`.${host}`));
                image.hidden = !approved;
                if (approved) { image.src = imageUrl.href; image.referrerPolicy = 'no-referrer'; }
            } catch { image.hidden = true; }
            infoReady = true;
        } catch (error) { showError(error); }
        finally { button.disabled = false; }
    });
    stage.addEventListener('pointerdown', (event) => {
        if (!aiToggle.checked || image.hidden || !image.complete || image.naturalHeight === 0 || event.button !== 0) return;
        const rect = image.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        event.preventDefault();
        dragPointerId = event.pointerId;
        dragStart = { x: Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width)), y: Math.max(0, Math.min(1, (event.clientY - rect.top) / rect.height)), rect };
    });
    window.addEventListener('pointermove', (event) => {
        if (!dragStart || event.pointerId !== dragPointerId) return;
        const { rect } = dragStart;
        const x = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
        const y = Math.max(0, Math.min(1, (event.clientY - rect.top) / rect.height));
        region.x = Math.min(dragStart.x, x); region.y = Math.min(dragStart.y, y);
        region.width = Math.abs(x - dragStart.x); region.height = Math.abs(y - dragStart.y);
        Object.assign(selection.style, { left: `${region.x * 100}%`, top: `${region.y * 100}%`, width: `${region.width * 100}%`, height: `${region.height * 100}%` });
        selection.hidden = false;
    });
    const finishSelection = (event) => {
        if (!dragStart || event.pointerId !== dragPointerId) return;
        dragStart = null;
        dragPointerId = null;
        if (region.width < .01 || region.height < .01) { resetRegion(); return; }
        clearRegion.hidden = false;
        regionHint.textContent = 'Đã chọn vùng. Kéo lại để đổi vùng hoặc bấm “Xóa vùng chọn”.';
    };
    window.addEventListener('pointerup', finishSelection);
    window.addEventListener('pointercancel', finishSelection);
    form.querySelector('[data-paste]').addEventListener('click', async () => {
        try { urlInput.value = await navigator.clipboard.readText(); }
        catch { urlInput.focus(); }
    });
    preview.querySelector('[data-download]').addEventListener('click', async (event) => {
        if (!infoReady || !form.querySelector('[name="rights"]').checked) return;
        if (aiToggle.checked && (region.width < .01 || region.height < .01)) {
            form.querySelector('[data-form-error]').textContent = 'Kéo chọn vùng watermark trước khi tạo tác vụ ProPainter.';
            return;
        }
        const button = event.currentTarget;
        button.disabled = true;
        try {
            const job = await post('/tiktok/download');
            currentId = job.id;
            progress.querySelector('[data-error]').textContent = '';
            updateJob({ ...job, progress: 0 });
            timer = setInterval(async () => {
                try {
                    const response = await fetch(`/tiktok/status/${encodeURIComponent(currentId)}`);
                    if (!response.ok) throw new Error('Không đọc được trạng thái tác vụ.');
                    const state = await response.json();
                    updateJob(state);
                    if (['Completed', 'Failed', 'Cancelled'].includes(state.status)) {
                        clearInterval(timer); timer = null; addHistory({ ...state, createdUtc: new Date().toISOString() });
                    }
                } catch (error) { progress.querySelector('[data-error]').textContent = error.message; clearInterval(timer); timer = null; }
            }, 1500);
        } catch (error) { showError(error); }
        finally { button.disabled = false; }
    });
    progress.querySelector('[data-cancel]').addEventListener('click', async () => {
        if (!currentId) return;
        try {
            const response = await fetch(`/tiktok/cancel/${encodeURIComponent(currentId)}`, {
                method: 'POST', headers: { RequestVerificationToken: token }
            });
            if (!response.ok) throw new Error('Không thể hủy tác vụ.');
            progress.querySelector('[data-status]').textContent = 'Cancelling';
        } catch (error) { progress.querySelector('[data-error]').textContent = error.message; }
    });
})();
