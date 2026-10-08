const stoppedCyclesChartContainer = document.querySelector("#stoppedCycles-chart");
const stoppedCyclesPercentage = stoppedCyclesChartContainer.dataset.percent;

var stoppedCyclesChartOptions = {
    series: [stoppedCyclesPercentage],
    colors: [getComputedStyle(document.documentElement).getPropertyValue("--tps-stopped").trim() || "#64708a"],
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
var stoppedCyclesChart = new ApexCharts(stoppedCyclesChartContainer, stoppedCyclesChartOptions);
stoppedCyclesChart.render();