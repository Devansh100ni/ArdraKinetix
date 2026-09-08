/**
 * ArdraKinetix Enterprise - Task Details Interactivity
 * Handles Comments, File Attachments, and Modal Previews
 */

async function submitComment(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    try {
        const res = await fetch('/Tasks/AddComment', {
            method: 'POST',
            body: formData
        });
        const data = await res.json();
        if (data.success) {
            window.TaskBoard?.showToast('Comment posted successfully.', 'success');
            setTimeout(() => window.location.reload(), 500);
        } else {
            window.TaskBoard?.showToast(data.error || 'Failed to post comment.', 'error');
        }
    } catch (err) {
        window.TaskBoard?.showToast('Network error while posting comment.', 'error');
    }
}

async function deleteComment(id) {
    if (!confirm('Are you sure you want to delete this comment?')) return;
    const token = window.TaskBoard?.getAntiForgeryToken() || '';
    const formData = new FormData();
    formData.append('commentId', id);
    formData.append('__RequestVerificationToken', token);

    try {
        const res = await fetch('/Tasks/DeleteComment', {
            method: 'POST',
            body: formData
        });
        const data = await res.json();
        if (data.success) {
            document.getElementById('comment-' + id)?.remove();
            window.TaskBoard?.showToast('Comment deleted.', 'success');
        } else {
            window.TaskBoard?.showToast(data.error || 'Failed to delete comment.', 'error');
        }
    } catch (err) {
        window.TaskBoard?.showToast('Network error while deleting comment.', 'error');
    }
}

async function submitAttachment(e) {
    e.preventDefault();
    const form = e.target;
    const formData = new FormData(form);

    try {
        const res = await fetch('/Tasks/UploadAttachment', {
            method: 'POST',
            body: formData
        });
        const data = await res.json();
        if (data.success) {
            window.TaskBoard?.showToast('File uploaded successfully.', 'success');
            setTimeout(() => window.location.reload(), 500);
        } else {
            window.TaskBoard?.showToast(data.error || 'Upload failed.', 'error');
        }
    } catch (err) {
        window.TaskBoard?.showToast('Network error while uploading file.', 'error');
    }
}

async function deleteAttachment(id) {
    if (!confirm('Are you sure you want to delete this attachment?')) return;
    const token = window.TaskBoard?.getAntiForgeryToken() || '';
    const formData = new FormData();
    formData.append('id', id);
    formData.append('__RequestVerificationToken', token);

    try {
        const res = await fetch('/Tasks/DeleteAttachment', {
            method: 'POST',
            body: formData
        });
        const data = await res.json();
        if (data.success) {
            document.getElementById('attachment-' + id)?.remove();
            window.TaskBoard?.showToast('Attachment deleted.', 'success');
        } else {
            window.TaskBoard?.showToast(data.error || 'Failed to delete attachment.', 'error');
        }
    } catch (err) {
        window.TaskBoard?.showToast('Network error while deleting attachment.', 'error');
    }
}

function openPreviewModal(url, title, isImage) {
    const titleEl = document.getElementById('previewModalTitle');
    const container = document.getElementById('previewModalContent');
    if (titleEl) titleEl.textContent = title;
    if (container) {
        if (isImage) {
            container.innerHTML = `<img src="${url}" class="max-h-[70vh] rounded-xl shadow-xl object-contain mx-auto" />`;
        } else {
            container.innerHTML = `<iframe src="${url}" class="w-full h-[70vh] rounded-xl border border-slate-200"></iframe>`;
        }
    }
    window.TaskBoard?.openModal('previewModal');
}
