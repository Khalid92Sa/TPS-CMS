const onHoldChartContainer = document.querySelector("#onhold-chart");
const onHoldPercentage = onHoldChartContainer.dataset.percent;

var onHoldChartOptions = {
    series: [onHoldPercentage],
    colors: [getComputedStyle(document.documentElement).getPropertyValue("--tps-status-hold").trim() || "#6a4fa3"],
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
var onHoldChart = new ApexCharts(onHoldChartContainer, onHoldChartOptions);
onHoldChart.render();