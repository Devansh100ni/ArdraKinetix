// ==========================================================
// realtime-notifications.js - ArdraKinetix Real-time SignalR Hub
// ==========================================================

function getAuthCookie() {
    const name = 'TaskBoard_Auth_Token';
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
        return decodeURIComponent(parts.pop().split(';').shift() || '');
    }
    return '';
}

document.addEventListener('alpine:init', () => {
    Alpine.data('notificationCenter', () => ({
        open: false,
        unreadCount: 0,
        notifications: [],
        isLoading: false,
        isConnected: false,

        init() {
            this.fetchNotifications();
            this.initSignalR();
        },

        async fetchNotifications() {
            try {
                this.isLoading = true;
                const response = await fetch('/api/notifications', {
                    headers: { 'Accept': 'application/json' }
                });
                if (response.ok) {
                    const data = await response.json();
                    this.unreadCount = data.unreadCount || 0;
                    this.notifications = data.notifications || [];
                }
            } catch (err) {
                console.error('Failed to load notifications', err);
            } finally {
                this.isLoading = false;
            }
        },

        async markAsRead(id, targetUrl) {
            try {
                const response = await fetch(`/api/notifications/${id}/read`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    }
                });

                if (response.ok) {
                    const n = this.notifications.find(x => x.id === id);
                    if (n && !n.isRead) {
                        n.isRead = true;
                        this.unreadCount = Math.max(0, this.unreadCount - 1);
                    }
                }
            } catch (err) {
                console.error('Failed to mark notification as read', err);
            }

            if (targetUrl && targetUrl.trim() !== '') {
                window.location.href = encodeURI(targetUrl);
            }
        },

        async markAllAsRead() {
            try {
                const response = await fetch('/api/notifications/read-all', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    }
                });

                if (response.ok) {
                    this.unreadCount = 0;
                    this.notifications.forEach(n => n.isRead = true);
                }
            } catch (err) {
                console.error('Failed to mark all notifications as read', err);
            }
        },

        initSignalR() {
            if (typeof signalR === 'undefined') {
                console.warn('SignalR library not loaded.');
                return;
            }

            const connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/taskboard', {
                    accessTokenFactory: () => {
                        return getAuthCookie();
                    },
                    skipNegotiation: false,
                    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling
                })
                .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Expose globally for other scripts (e.g. Scrum Board real-time drag/drop sync)
            window.taskBoardHubConnection = connection;

            // 1. Receive User Notification
            connection.on('ReceiveNotification', (notification) => {
                this.unreadCount++;
                this.notifications.unshift(notification);
                if (this.notifications.length > 20) {
                    this.notifications.pop();
                }

                // Show toast alert if available
                if (window.showToast) {
                    window.showToast(notification.title, notification.message, 'info');
                }
            });

            // 2. Receive Force Logout Signal
            connection.on('ForceLogout', (data) => {
                console.warn('Received ForceLogout signal:', data);
                connection.stop();
                window.location.href = '/Account/Login?reason=session_revoked';
            });

            // 3. Receive Task Updates / Real-time Scrum Moves
            connection.on('TaskMoved', (data) => {
                console.log('Real-time TaskMoved:', data);
                if (window.onSignalRTaskMoved) {
                    window.onSignalRTaskMoved(data);
                }
            });

            connection.on('TaskUpdated', (data) => {
                console.log('Real-time TaskUpdated:', data);
                if (window.onSignalRTaskUpdated) {
                    window.onSignalRTaskUpdated(data);
                }
            });

            connection.onreconnecting((error) => {
                console.warn('SignalR reconnecting...', error);
                this.isConnected = false;
            });

            connection.onreconnected((connectionId) => {
                console.log('SignalR reconnected. ConnectionId:', connectionId);
                this.isConnected = true;
                this.fetchNotifications();
            });

            connection.onclose((error) => {
                console.warn('SignalR connection closed.', error);
                this.isConnected = false;
            });

            connection.start()
                .then(() => {
                    console.log('SignalR successfully connected to /hubs/taskboard');
                    this.isConnected = true;
                })
                .catch(err => {
                    console.error('SignalR initial connection failed:', err);
                    this.isConnected = false;
                });
        }
    }));
});
