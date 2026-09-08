// Dynamic HTML5 Drag-and-Drop Scrum Board Controller with Real-Time SignalR Sync

document.addEventListener('DOMContentLoaded', function () {
    const boardContainer = document.getElementById('scrum-board-container');
    if (!boardContainer) return;

    let draggedCard = null;
    let sourceColumn = null;

    // 1. Join SignalR Tenant Group for Real-Time Sync
    function joinBoardTenantGroup() {
        const tenantId = boardContainer.dataset.tenantId;
        if (!tenantId || tenantId.trim() === '') return;

        const checkAndJoin = () => {
            if (window.taskBoardHubConnection && window.taskBoardHubConnection.state === 'Connected') {
                window.taskBoardHubConnection.invoke('JoinTenant', tenantId)
                    .then(() => console.log('Joined real-time SignalR tenant board group:', tenantId))
                    .catch(err => console.warn('Failed to join SignalR tenant group:', err));
            }
        };

        // Try immediate join or retry when connection becomes active
        checkAndJoin();
        const interval = setInterval(() => {
            if (window.taskBoardHubConnection && window.taskBoardHubConnection.state === 'Connected') {
                checkAndJoin();
                clearInterval(interval);
            }
        }, 1000);

        // Clear check after 15 seconds
        setTimeout(() => clearInterval(interval), 15000);
    }

    joinBoardTenantGroup();

    // 2. Initialize draggable cards
    function initDraggables() {
        const cards = document.querySelectorAll('.scrum-task-card');
        cards.forEach(card => {
            card.setAttribute('draggable', 'true');
            card.removeEventListener('dragstart', handleDragStart);
            card.removeEventListener('dragend', handleDragEnd);
            card.addEventListener('dragstart', handleDragStart);
            card.addEventListener('dragend', handleDragEnd);
        });

        const dropZones = document.querySelectorAll('.scrum-drop-zone');
        dropZones.forEach(zone => {
            zone.removeEventListener('dragover', handleDragOver);
            zone.removeEventListener('dragenter', handleDragEnter);
            zone.removeEventListener('dragleave', handleDragLeave);
            zone.removeEventListener('drop', handleDrop);
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

        // Remove placeholder from target zone if present
        const targetPlaceholder = this.querySelector('.empty-column-placeholder');
        if (targetPlaceholder) targetPlaceholder.remove();

        // Optimistically move card in UI
        const sourceDropZone = draggedCard.closest('.scrum-drop-zone');
        const placeholder = document.createElement('div');
        draggedCard.parentNode.insertBefore(placeholder, draggedCard);
        this.appendChild(draggedCard);

        // If source column is now empty, add placeholder
        if (sourceDropZone && sourceDropZone.querySelectorAll('.scrum-task-card').length === 0) {
            const ph = document.createElement('div');
            ph.className = 'empty-column-placeholder h-24 border border-dashed border-slate-300 rounded-xl flex items-center justify-center text-slate-400 text-xs italic bg-white/40';
            ph.textContent = 'Drop task here';
            sourceDropZone.appendChild(ph);
        }

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
        if (!column) return;
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

    // 3. Real-Time SignalR Event Receiver: TaskMoved
    window.onSignalRTaskMoved = function (data) {
        if (!data) return;

        const taskId = data.taskId;
        const taskNumber = data.taskNumber;
        const targetStatusId = data.targetStatusId;

        // Locate the task card on the board
        let card = null;
        if (taskId) {
            card = document.querySelector(`.scrum-task-card[data-task-id="${taskId}"]`);
        }
        if (!card && taskNumber) {
            const allCards = document.querySelectorAll('.scrum-task-card');
            for (const c of allCards) {
                const numEl = c.querySelector('.task-number');
                if (numEl && numEl.textContent.trim() === taskNumber.trim()) {
                    card = c;
                    break;
                }
            }
        }

        if (!card) {
            console.log('Real-time task not present on current board view:', taskNumber);
            return;
        }

        // Locate target column drop zone
        let targetZone = null;
        if (targetStatusId) {
            targetZone = document.querySelector(`.scrum-drop-zone[data-status-id="${targetStatusId}"]`);
        }
        if (!targetZone && data.newStatus) {
            const col = document.querySelector(`.scrum-column[data-column-name="${data.newStatus}"]`);
            if (col) targetZone = col.querySelector('.scrum-drop-zone');
        }

        if (!targetZone) {
            console.warn('Target status drop zone not found for:', targetStatusId, data.newStatus);
            return;
        }

        // If card is already in the target zone, skip
        if (card.parentNode === targetZone) {
            return;
        }

        const sourceZone = card.closest('.scrum-drop-zone');

        // Remove placeholder from target column
        const targetPlaceholder = targetZone.querySelector('.empty-column-placeholder');
        if (targetPlaceholder) targetPlaceholder.remove();

        // Move card to target column
        targetZone.appendChild(card);
        card.dataset.statusId = targetStatusId || '';

        // Check if source column is now empty
        if (sourceZone && sourceZone.querySelectorAll('.scrum-task-card').length === 0) {
            const ph = document.createElement('div');
            ph.className = 'empty-column-placeholder h-24 border border-dashed border-slate-300 rounded-xl flex items-center justify-center text-slate-400 text-xs italic bg-white/40';
            ph.textContent = 'Drop task here';
            sourceZone.appendChild(ph);
        }

        // Visual flash highlight
        card.classList.add('ring-2', 'ring-sky-400', 'bg-sky-50/70', 'transition-all');
        setTimeout(() => {
            card.classList.remove('ring-2', 'ring-sky-400', 'bg-sky-50/70');
        }, 2000);

        // Update counts and check WIP limits
        updateColumnCounts();
        checkWipLimits(targetZone.closest('.scrum-column'));
        if (sourceZone) checkWipLimits(sourceZone.closest('.scrum-column'));

        // Show toast notification
        if (window.showToast && data.updatedBy) {
            window.showToast('Scrum Board Updated', `${taskNumber || 'Task'} moved to ${data.newStatus} by ${data.updatedBy}`, 'info');
        }
    };

    initDraggables();
});
