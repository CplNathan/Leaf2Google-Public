const geojson = {
    'type': 'FeatureCollection',
    'features': [
        {
            'type': 'Feature',
            'properties': {
                'message': 'Leaf',
                'iconSize': [64, 64]
            },
            'geometry': {
                'type': 'Point',
                'coordinates': mbcoords
                        }
                    }
                ]
            };

const map = new mapboxgl.Map({
    container: 'map',
    style: 'mapbox://styles/mapbox/streets-v11',
    center: mbcoords,
    zoom: 10
});

// Add markers to the map.
for (const marker of geojson.features) {
    // Create a DOM element for each marker.
    const el = document.createElement('div');
    const width = marker.properties.iconSize[0];
    const height = marker.properties.iconSize[1];
    el.className = 'marker';
    el.style.backgroundImage = `url(img/leaf.png)`;
    el.style.width = `${width}px`;
    el.style.height = `${height}px`;
    el.style.backgroundSize = '100%';

    el.addEventListener('click', () => {
        window.alert(marker.properties.message);
    });

    // Add markers to the map.
    new mapboxgl.Marker(el)
        .setLngLat(marker.geometry.coordinates)
        .addTo(map);
}