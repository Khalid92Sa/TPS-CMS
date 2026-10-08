const mapContainer = document.querySelector('#candidate-sources-map');
const locations = JSON.parse(mapContainer.dataset.locations.replaceAll("'",'"')) ?? [];

const markers = locations.map(l=>{
    const country = countries[l];
    return { name: l, coords: country.coord, shortname: country.shortname}
})


const tpsToken = (name, fallback) => getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;

const  worldemapmarkers = new jsVectorMap({
    map: "world_merc",
    selector: "#candidate-sources-map",
    zoomOnScroll: true,
    zoomButtons: false,
    showTooltip: true,
    focusOn: {
        regions: markers.map(x => x.shortname),
        animate: true
    },
    selectedMarkers: [],
    regionStyle: {
      initial: {
        stroke: tpsToken("--tps-border-strong", "#cdd5e1"),
        strokeWidth: 0.25,
        fill: tpsToken("--tps-surface-3", "#eef2f7"),
        fillOpacity: 1,
      },
    },
    markersSelectable: false,
    markers: markers,
    markerStyle: { initial: { fill: tpsToken("--tps-primary", "#2856c8") }, selected: { fill: tpsToken("--tps-accent", "#128a88") } },
    labels: {
      markers: {
        render: function (e) {
          return e.name;
        },
      },
    },
  });
