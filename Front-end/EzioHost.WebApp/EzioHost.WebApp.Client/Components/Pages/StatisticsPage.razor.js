// Chart instances storage
const chartInstances = {};

// Color palette for charts
const colors = {
    primary: "#6366f1",
    primaryLight: "#818cf8",
    primaryDark: "#4f46e5",
    success: "#10b981",
    warning: "#f59e0b",
    danger: "#ef4444",
    info: "#3b82f6",
    pieColors: [
        "#6366f1", // primary
        "#10b981", // success
        "#f59e0b", // warning
        "#ef4444", // danger
        "#3b82f6", // info
        "#8b5cf6", // purple
        "#ec4899", // pink
        "#06b6d4", // cyan
    ]
};

// Format storage bytes to human readable
function formatStorage(bytes) {
    if (bytes >= 1024 * 1024 * 1024 * 1024) {
        return (bytes / (1024 * 1024 * 1024 * 1024)).toFixed(2) + " TB";
    }
    if (bytes >= 1024 * 1024 * 1024) {
        return (bytes / (1024 * 1024 * 1024)).toFixed(2) + " GB";
    }
    if (bytes >= 1024 * 1024) {
        return (bytes / (1024 * 1024)).toFixed(2) + " MB";
    }
    return (bytes / 1024).toFixed(2) + " KB";
}

// Initialize Video Timeline Chart (Line Chart)
export function initVideoTimelineChart(canvasId, labels, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) {
        console.error("Canvas element or Chart.js not found");
        return;
    }

    // Destroy existing chart if any
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const ctx = canvas.getContext("2d");
    const chart = new Chart(ctx,
        {
            type: "line",
            data: {
                labels: labels,
                datasets: [
                    {
                        label: "Số lượng video",
                        data: data,
                        borderColor: colors.primary,
                        backgroundColor: colors.primary + "20",
                        borderWidth: 3,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6,
                        pointBackgroundColor: colors.primary,
                        pointBorderColor: "#fff",
                        pointBorderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: "top"
                    },
                    tooltip: {
                        mode: "index",
                        intersect: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    },
                    x: {
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45
                        }
                    }
                }
            }
        });

    chartInstances[canvasId] = chart;
}

// Initialize Storage Timeline Chart (Line Chart)
export function initStorageTimelineChart(canvasId, labels, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) {
        console.error("Canvas element or Chart.js not found");
        return;
    }

    // Destroy existing chart if any
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const ctx = canvas.getContext("2d");
    const chart = new Chart(ctx,
        {
            type: "line",
            data: {
                labels: labels,
                datasets: [
                    {
                        label: "Dung lượng lưu trữ",
                        data: data,
                        borderColor: colors.success,
                        backgroundColor: colors.success + "20",
                        borderWidth: 3,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 4,
                        pointHoverRadius: 6,
                        pointBackgroundColor: colors.success,
                        pointBorderColor: "#fff",
                        pointBorderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: "top"
                    },
                    tooltip: {
                        mode: "index",
                        intersect: false,
                        callbacks: {
                            label: function(context) {
                                return `Dung lượng: ${formatStorage(context.parsed.y)}`;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return formatStorage(value);
                            }
                        }
                    },
                    x: {
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45
                        }
                    }
                }
            }
        });

    chartInstances[canvasId] = chart;
}

// Initialize Resolution Pie Chart
export function initResolutionPieChart(canvasId, labels, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) {
        console.error("Canvas element or Chart.js not found");
        return;
    }

    // Destroy existing chart if any
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const ctx = canvas.getContext("2d");
    const backgroundColors = colors.pieColors.slice(0, labels.length);

    const chart = new Chart(ctx,
        {
            type: "pie",
            data: {
                labels: labels,
                datasets: [
                    {
                        data: data,
                        backgroundColor: backgroundColors,
                        borderColor: "#fff",
                        borderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: "right"
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || "";
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                return label + ": " + value + " (" + percentage + "%)";
                            }
                        }
                    }
                }
            }
        });

    chartInstances[canvasId] = chart;
}

// Initialize Status Pie Chart
export function initStatusPieChart(canvasId, labels, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) {
        console.error("Canvas element or Chart.js not found");
        return;
    }

    // Destroy existing chart if any
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const ctx = canvas.getContext("2d");
    const backgroundColors = colors.pieColors.slice(0, labels.length);

    const chart = new Chart(ctx,
        {
            type: "pie",
            data: {
                labels: labels,
                datasets: [
                    {
                        data: data,
                        backgroundColor: backgroundColors,
                        borderColor: "#fff",
                        borderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: true,
                plugins: {
                    legend: {
                        display: true,
                        position: "right"
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const label = context.label || "";
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                return label + ": " + value + " (" + percentage + "%)";
                            }
                        }
                    }
                }
            }
        });

    chartInstances[canvasId] = chart;
}

// Destroy chart instance
export function destroyChart(canvasId) {
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
        delete chartInstances[canvasId];
    }
}