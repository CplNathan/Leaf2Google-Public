class LocationMap extends HTMLElement {
    static get observedAttributes() {
        return ['lat', 'long'];
    }

    constructor() {
        // Always call super first in constructor
        super();

        const shadow = this.attachShadow({ mode: 'open' });

        const linkBootstrap = $('<link>', {
            rel: 'stylesheet',
            href: '/css/bundle.css'
        }).appendTo(shadow);

        const linkMapbox = $('<link>', {
            rel: 'stylesheet',
            href: 'https://api.mapbox.com/mapbox-gl-js/v2.10.0/mapbox-gl.css'
        }).appendTo(shadow);

        const map = $('<div>', {
            id: 'map',
            class: 'h-100'
        }).appendTo(shadow);
    }

    connectedCallback() {
        const elem = this;
        const shadow = elem.shadowRoot;

        mapboxgl.accessToken = $(elem).attr('key');

        elem.map = new mapboxgl.Map({
            container: $(shadow).find('#map')[0],
            style: 'mapbox://styles/mapbox/dark-v10',
            center: [$(elem).attr('long') ?? 0, $(elem).attr('lat') ?? 0],
            zoom: 10,
            trackResize: true
        });

        elem.mapgeojson = {
            'type': 'FeatureCollection',
            'features': [
                {
                    'type': 'Feature',
                    'properties': {
                        'message': 'Leaf',
                        'iconSize': [128, 64]
                    },
                    'geometry': {
                        'type': 'Point',
                        'coordinates': [$(elem).attr('long') ?? 0, $(elem).attr('lat') ?? 0]
                    }
                }
            ]
        };

        // Add markers to the map.
        for (const marker of elem.mapgeojson.features) {
            // Create a DOM element for each marker.
            const el = document.createElement('div');
            const width = marker.properties.iconSize[0];
            const height = marker.properties.iconSize[1];
            el.className = 'marker';
            el.style.backgroundImage = `url('` + $(elem).attr('icon') + `')`;
            el.style.width = `${width}px`;
            el.style.height = `${height}px`;
            el.style.backgroundSize = '100%';

            el.addEventListener('click', () => {
                window.alert(marker.properties.message);
            });

            // Add markers to the map.
            elem.mapicon = new mapboxgl.Marker(el)
                .setLngLat(marker.geometry.coordinates)
                .addTo(elem.map);
        }

        elem.map.on('idle', function () {
            elem.map.resize()
        })
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {
        this.mapicon?.remove();

        this.mapgeojson = {
            'type': 'FeatureCollection',
            'features': [
                {
                    'type': 'Feature',
                    'properties': {
                        'message': 'Leaf',
                        'iconSize': [128, 64]
                    },
                    'geometry': {
                        'type': 'Point',
                        'coordinates': [$(this).attr('long') ?? 0, $(this).attr('lat') ?? 0]
                    }
                }
            ]
        };

        // Add markers to the map.
        for (const marker of this.mapgeojson.features) {
            // Create a DOM element for each marker.
            const el = document.createElement('div');
            const width = marker.properties.iconSize[0];
            const height = marker.properties.iconSize[1];
            el.className = 'marker';
            el.style.backgroundImage = `url('` + $(this).attr('icon') + `')`;
            el.style.width = `${width}px`;
            el.style.height = `${height}px`;
            el.style.backgroundSize = '100%';

            el.addEventListener('click', () => {
                window.alert(marker.properties.message);
            });

            // Add markers to the map.
            this.mapicon = new mapboxgl.Marker(el)
                .setLngLat(marker.geometry.coordinates)
                .addTo(this.map);
        }
    }
}

customElements.define('l2g-locationmap', LocationMap);