// Dynamic HTML5 Drag-and-Drop Scrum Board Controller

document.addEventListener('DOMContentLoaded', function () {
    const boardContainer = document.getElementById('scrum-board-container');
    if (!boardContainer) return;

    let draggedCard = null;
    let sourceColumn = null;

    // Initialize draggable cards
    function initDraggables() {
        const cards = document.querySelectorAll('.scrum-task-card');
        cards.forEach(card => {
            card.setAttribute('draggable', 'true');

            card.addEventListener('dragstart', handleDragStart);
            card.addEventListener('dragend', handleDragEnd);
        });

        const dropZones = document.querySelectorAll('.scrum-drop-zone');
        dropZones.forEach(zone => {
            zone.addEventListener('dragover', handleDragOver);
            zone.addEventListener('dragenter', handleDragEnter);
            zone.addEventListener('dragleave', handleDragLeave);
            zone.addEventListener('drop', handleDrop);
        });
    }

    function handleDragStart(e) {
        draggedCard = this;
        sourceColumn = this.closest('.scrum-column');
        this.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', this.dataset.taskId);
    }

    function handleDragEnd(e) {
        this.classList.remove('dragging');
        document.querySelectorAll('.scrum-drop-zone').forEach(zone => {
            zone.classList.remove('drop-target-active');
        });
    }

    function handleDragOver(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    }

    function handleDragEnter(e) {
        e.preventDefault();
        this.classList.add('drop-target-active');
    }

    function handleDragLeave(e) {
        this.classList.remove('drop-target-active');
    }

    async function handleDrop(e) {
        e.preventDefault();
        this.classList.remove('drop-target-active');

        if (!draggedCard) return;

        const targetColumn = this.closest('.scrum-column');
        const targetStatusId = this.dataset.statusId;
        const currentStatusId = draggedCard.dataset.statusId;
        const taskId = draggedCard.dataset.taskId;

        if (targetStatusId === currentStatusId) {
            return;
        }

        // Optimistically move card in UI
        const placeholder = document.createElement('div');
        draggedCard.parentNode.insertBefore(placeholder, draggedCard);
        this.appendChild(draggedCard);

        // Update column counts
        updateColumnCounts();

        // Send AJAX Move request
        try {
            const token = window.TaskBoard.getAntiForgeryToken();
            const response = await fetch('/Tasks/Move', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({
                    taskId: taskId,
                    targetStatusId: targetStatusId
                })
            });

            const data = await response.json();

            if (data.success) {
                draggedCard.dataset.statusId = targetStatusId;
                placeholder.remove();

                const taskNumber = draggedCard.querySelector('.task-number')?.textContent || 'Task';
                window.TaskBoard.showToast(`${taskNumber} moved to ${targetColumn.dataset.columnName}`, 'success');

                // Check WIP limit
                checkWipLimits(targetColumn);
            } else {
                // Revert move
                placeholder.parentNode.insertBefore(draggedCard, placeholder);
                placeholder.remove();
                updateColumnCounts();
                window.TaskBoard.showToast(data.error || 'Failed to move task.', 'error');
            }
        } catch (err) {
            // Revert move on network/server error
            placeholder.parentNode.insertBefore(draggedCard, placeholder);
            placeholder.remove();
            updateColumnCounts();
            window.TaskBoard.showToast('Network error while moving task.', 'error');
        }
    }

    function updateColumnCounts() {
        document.querySelectorAll('.scrum-column').forEach(col => {
            const zone = col.querySelector('.scrum-drop-zone');
            const countBadge = col.querySelector('.column-task-count');
            if (zone && countBadge) {
                const count = zone.querySelectorAll('.scrum-task-card').length;
                countBadge.textContent = `${count}`;
            }
        });
    }

    function checkWipLimits(column) {
        const wipLimit = parseInt(column.dataset.wipLimit, 10);
        if (!isNaN(wipLimit) && wipLimit > 0) {
            const count = column.querySelectorAll('.scrum-task-card').length;
            const limitBadge = column.querySelector('.wip-limit-warning');
            if (count > wipLimit) {
                if (limitBadge) limitBadge.classList.remove('hidden');
            } else {
                if (limitBadge) limitBadge.classList.add('hidden');
            }
        }
    }

    initDraggables();
});
