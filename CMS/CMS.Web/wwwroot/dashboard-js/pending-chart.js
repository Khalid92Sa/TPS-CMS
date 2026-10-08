const pendingChartContainer = document.querySelector("#pending-chart");
const pendingPercentage = pendingChartContainer.dataset.percent;

var pendingChartOptions = {
    series: [pendingPercentage],
    colors: [getComputedStyle(document.documentElement).getPropertyValue("--tps-status-pending").trim() || "#a8660a"],
  chart: { type: "radialBar", width: 90, sparkline: { enabled: true } },
    dataLabels: { enabled: false },
    plotOptions: {
        radialBar: {
            hollow: { margin: 0, size: "70%" },
            track: { margin: 1, background: getComputedStyle(document.documentElement).getPropertyValue("--tps-surface-3").trim() || "#eef2f7" },
            dataLabels: {
                show: true,
                name: { show: false },
                value: { show: true, fontSize: "16px", fontWeight: 600, offsetY: 8 },
            },
        },
    }
};
var pendingChart = new ApexCharts(pendingChartContainer, pendingChartOptions);
pendingChart.render();