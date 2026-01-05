/**
 * RAL Color Preview - Three.js Component
 * Visualizes RAL colors on powder-coated aluminum surfaces
 *
 * Usage:
 *   const preview = RalPreview.create(container, {
 *     model: 'cube' | 'window-frame' | { object3D },
 *     finish: 'matte' | 'satin' | 'gloss',
 *     color: { hex: '#383E42' },
 *     lrv: 8,
 *     controls: { orbit: true, autoRotate: true },
 *     background: 'transparent' | '#ffffff',
 *     powder: { intensity: 0.4, scale: 0.8 }
 *   });
 */
(function () {
    'use strict';

    // Ensure THREE is available
    if (typeof THREE === 'undefined') {
        console.error('RalPreview: THREE.js is required but not loaded');
        return;
    }

    // ============================================
    // CONSTANTS & PRESETS
    // ============================================

    const FINISH_PRESETS = {
        matte: {
            roughness: 0.85,
            metalness: 0.1,
            clearcoat: 0.0,
            clearcoatRoughness: 0.0,
            reflectivity: 0.2,
            envMapIntensity: 0.3
        },
        satin: {
            roughness: 0.55,
            metalness: 0.15,
            clearcoat: 0.15,
            clearcoatRoughness: 0.6,
            reflectivity: 0.4,
            envMapIntensity: 0.5
        },
        gloss: {
            roughness: 0.15,
            metalness: 0.2,
            clearcoat: 0.8,
            clearcoatRoughness: 0.1,
            reflectivity: 0.8,
            envMapIntensity: 0.8
        }
    };

    const DEFAULT_OPTIONS = {
        model: 'cube',
        finish: 'satin',
        color: { hex: '#888888' },
        lrv: 50,
        controls: { orbit: true, autoRotate: false, zoom: true, pan: false },
        background: 'transparent',
        powder: { intensity: 0.3, scale: 1.0 }
    };

    // ============================================
    // TEXTURE GENERATOR
    // ============================================

    class TextureGenerator {
        constructor() {
            this.canvas = document.createElement('canvas');
            this.ctx = this.canvas.getContext('2d');
        }

        // Simple value noise function
        _noise(x, y) {
            const hash = (a, b) => {
                const n = Math.sin(a * 12.9898 + b * 78.233) * 43758.5453;
                return n - Math.floor(n);
            };

            const ix = Math.floor(x), iy = Math.floor(y);
            const fx = x - ix, fy = y - iy;

            const smooth = t => t * t * (3 - 2 * t);
            const sx = smooth(fx), sy = smooth(fy);

            const v00 = hash(ix, iy);
            const v10 = hash(ix + 1, iy);
            const v01 = hash(ix, iy + 1);
            const v11 = hash(ix + 1, iy + 1);

            return (v00 * (1 - sx) + v10 * sx) * (1 - sy) +
                (v01 * (1 - sx) + v11 * sx) * sy;
        }

        // Generate powder coat roughness texture
        createPowderRoughnessMap(options = {}) {
            const { size = 512, intensity = 0.3, scale = 1.0 } = options;
            this.canvas.width = size;
            this.canvas.height = size;

            const imageData = this.ctx.createImageData(size, size);
            const data = imageData.data;

            for (let y = 0; y < size; y++) {
                for (let x = 0; x < size; x++) {
                    const idx = (y * size + x) * 4;

                    // Multi-octave noise for powder effect
                    let noise = 0;
                    noise += this._noise(x * scale * 0.05, y * scale * 0.05) * 0.5;
                    noise += this._noise(x * scale * 0.15, y * scale * 0.15) * 0.3;
                    noise += this._noise(x * scale * 0.4, y * scale * 0.4) * 0.2;

                    // Fine particle granularity
                    noise += (Math.random() - 0.5) * 0.1;

                    const value = Math.floor(128 + (noise - 0.5) * intensity * 255);
                    data[idx] = data[idx + 1] = data[idx + 2] = Math.max(0, Math.min(255, value));
                    data[idx + 3] = 255;
                }
            }

            this.ctx.putImageData(imageData, 0, 0);

            const texture = new THREE.CanvasTexture(this.canvas);
            texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
            texture.repeat.set(2, 2);
            return texture;
        }

        // Generate normal map for micro-surface detail
        createPowderNormalMap(options = {}) {
            const { size = 512, strength = 0.15, scale = 1.0 } = options;
            this.canvas.width = size;
            this.canvas.height = size;

            // First generate heightmap
            const heightData = new Float32Array(size * size);
            for (let y = 0; y < size; y++) {
                for (let x = 0; x < size; x++) {
                    let noise = 0;
                    noise += this._noise(x * scale * 0.08, y * scale * 0.08) * 0.6;
                    noise += this._noise(x * scale * 0.2, y * scale * 0.2) * 0.3;
                    noise += this._noise(x * scale * 0.5, y * scale * 0.5) * 0.1;
                    heightData[y * size + x] = noise;
                }
            }

            // Convert to normal map using Sobel filter
            const imageData = this.ctx.createImageData(size, size);
            const data = imageData.data;

            for (let y = 0; y < size; y++) {
                for (let x = 0; x < size; x++) {
                    const idx = (y * size + x) * 4;

                    // Sample neighbors with wrapping
                    const getHeight = (px, py) => {
                        px = (px + size) % size;
                        py = (py + size) % size;
                        return heightData[py * size + px];
                    };

                    // Sobel filter for normal calculation
                    const left = getHeight(x - 1, y);
                    const right = getHeight(x + 1, y);
                    const up = getHeight(x, y - 1);
                    const down = getHeight(x, y + 1);

                    const nx = (left - right) * strength;
                    const ny = (up - down) * strength;
                    const nz = 1.0;

                    // Normalize
                    const len = Math.sqrt(nx * nx + ny * ny + nz * nz);

                    // Convert to RGB (0-255 range, centered at 128)
                    data[idx] = Math.floor((nx / len + 1) * 127.5);
                    data[idx + 1] = Math.floor((ny / len + 1) * 127.5);
                    data[idx + 2] = Math.floor((nz / len + 1) * 127.5);
                    data[idx + 3] = 255;
                }
            }

            this.ctx.putImageData(imageData, 0, 0);

            const texture = new THREE.CanvasTexture(this.canvas);
            texture.wrapS = texture.wrapT = THREE.RepeatWrapping;
            texture.repeat.set(2, 2);
            return texture;
        }
    }

    // ============================================
    // MODEL FACTORY
    // ============================================

    class ModelFactory {
        // Create beveled cube geometry
        static createBeveledCube(size = 1, bevelRadius = 0.04, segments = 8) {
            // Use RoundedBoxGeometry approach with subdivided box
            const geometry = new THREE.BoxGeometry(size, size, size, segments, segments, segments);
            const positions = geometry.attributes.position;
            const halfSize = size / 2;
            const innerSize = halfSize - bevelRadius;

            for (let i = 0; i < positions.count; i++) {
                let x = positions.getX(i);
                let y = positions.getY(i);
                let z = positions.getZ(i);

                // Calculate distance from center for each axis
                const ax = Math.abs(x);
                const ay = Math.abs(y);
                const az = Math.abs(z);

                // Check if vertex is in corner/edge region
                const inCornerX = ax > innerSize;
                const inCornerY = ay > innerSize;
                const inCornerZ = az > innerSize;

                if (inCornerX || inCornerY || inCornerZ) {
                    // Clamp to inner box
                    const cx = Math.sign(x) * Math.min(ax, innerSize);
                    const cy = Math.sign(y) * Math.min(ay, innerSize);
                    const cz = Math.sign(z) * Math.min(az, innerSize);

                    // Direction from clamped point to original
                    let dx = x - cx;
                    let dy = y - cy;
                    let dz = z - cz;

                    // Normalize and scale to bevel radius
                    const len = Math.sqrt(dx * dx + dy * dy + dz * dz);
                    if (len > 0.0001) {
                        dx /= len;
                        dy /= len;
                        dz /= len;

                        x = cx + dx * bevelRadius;
                        y = cy + dy * bevelRadius;
                        z = cz + dz * bevelRadius;
                    }

                    positions.setXYZ(i, x, y, z);
                }
            }

            geometry.computeVertexNormals();
            return geometry;
        }

        // Create window frame geometry
        static createWindowFrame(options = {}) {
            const {
                width = 1.0,
                height = 1.4,
                frameDepth = 0.07,
                frameWidth = 0.055,
                mullionWidth = 0.035
            } = options;

            const group = new THREE.Group();

            // Frame material placeholder (will be set by PreviewInstance)
            const frameMaterial = new THREE.MeshPhysicalMaterial({ color: 0x888888 });

            // Glass material
            const glassMaterial = new THREE.MeshPhysicalMaterial({
                color: 0x88ccff,
                transparent: true,
                opacity: 0.25,
                roughness: 0.05,
                metalness: 0,
                transmission: 0.85,
                thickness: 0.006,
                side: THREE.DoubleSide
            });

            // Gasket material
            const gasketMaterial = new THREE.MeshStandardMaterial({
                color: 0x1a1a1a,
                roughness: 0.85,
                metalness: 0
            });

            const hw = width / 2;
            const hh = height / 2;
            const hd = frameDepth / 2;

            // Helper to create frame segment
            const createFrameSegment = (w, h, d) => {
                return new THREE.BoxGeometry(w, h, d, 2, 2, 2);
            };

            // Top rail
            const topRail = new THREE.Mesh(
                createFrameSegment(width, frameWidth, frameDepth),
                frameMaterial
            );
            topRail.position.set(0, hh - frameWidth / 2, 0);
            topRail.userData.isFrame = true;
            group.add(topRail);

            // Bottom rail
            const bottomRail = new THREE.Mesh(
                createFrameSegment(width, frameWidth, frameDepth),
                frameMaterial
            );
            bottomRail.position.set(0, -hh + frameWidth / 2, 0);
            bottomRail.userData.isFrame = true;
            group.add(bottomRail);

            // Left stile
            const leftStile = new THREE.Mesh(
                createFrameSegment(frameWidth, height - frameWidth * 2, frameDepth),
                frameMaterial
            );
            leftStile.position.set(-hw + frameWidth / 2, 0, 0);
            leftStile.userData.isFrame = true;
            group.add(leftStile);

            // Right stile
            const rightStile = new THREE.Mesh(
                createFrameSegment(frameWidth, height - frameWidth * 2, frameDepth),
                frameMaterial
            );
            rightStile.position.set(hw - frameWidth / 2, 0, 0);
            rightStile.userData.isFrame = true;
            group.add(rightStile);

            // Central horizontal mullion
            const mullion = new THREE.Mesh(
                createFrameSegment(width - frameWidth * 2, mullionWidth, frameDepth * 0.75),
                frameMaterial
            );
            mullion.position.set(0, 0, 0);
            mullion.userData.isFrame = true;
            group.add(mullion);

            // Glass panes
            const glassWidth = width - frameWidth * 2 - 0.008;
            const glassHeight = (height - frameWidth * 2 - mullionWidth) / 2 - 0.004;

            // Upper glass pane
            const upperGlass = new THREE.Mesh(
                new THREE.PlaneGeometry(glassWidth, glassHeight),
                glassMaterial
            );
            upperGlass.position.set(0, glassHeight / 2 + mullionWidth / 2 + 0.002, -hd + 0.015);
            group.add(upperGlass);

            // Lower glass pane
            const lowerGlass = new THREE.Mesh(
                new THREE.PlaneGeometry(glassWidth, glassHeight),
                glassMaterial
            );
            lowerGlass.position.set(0, -glassHeight / 2 - mullionWidth / 2 - 0.002, -hd + 0.015);
            group.add(lowerGlass);

            // Gasket strips (thin lines around glass)
            const gasketThickness = 0.004;
            const gasketDepth = 0.008;

            const createGasketStrip = (w, h, x, y) => {
                const strip = new THREE.Mesh(
                    new THREE.BoxGeometry(w, h, gasketDepth),
                    gasketMaterial
                );
                strip.position.set(x, y, -hd + 0.008);
                return strip;
            };

            // Upper pane gaskets
            const upperY = glassHeight / 2 + mullionWidth / 2 + 0.002;
            group.add(createGasketStrip(glassWidth + gasketThickness * 2, gasketThickness, 0, upperY + glassHeight / 2)); // top
            group.add(createGasketStrip(glassWidth + gasketThickness * 2, gasketThickness, 0, upperY - glassHeight / 2)); // bottom
            group.add(createGasketStrip(gasketThickness, glassHeight, -glassWidth / 2 - gasketThickness / 2, upperY)); // left
            group.add(createGasketStrip(gasketThickness, glassHeight, glassWidth / 2 + gasketThickness / 2, upperY)); // right

            // Lower pane gaskets
            const lowerY = -glassHeight / 2 - mullionWidth / 2 - 0.002;
            group.add(createGasketStrip(glassWidth + gasketThickness * 2, gasketThickness, 0, lowerY + glassHeight / 2)); // top
            group.add(createGasketStrip(glassWidth + gasketThickness * 2, gasketThickness, 0, lowerY - glassHeight / 2)); // bottom
            group.add(createGasketStrip(gasketThickness, glassHeight, -glassWidth / 2 - gasketThickness / 2, lowerY)); // left
            group.add(createGasketStrip(gasketThickness, glassHeight, glassWidth / 2 + gasketThickness / 2, lowerY)); // right

            return group;
        }

        // Create door geometry
        static createDoor(options = {}) {
            const {
                width = 0.9,
                height = 2.0,
                depth = 0.045,
                frameWidth = 0.06,
                panelInset = 0.008
            } = options;

            const group = new THREE.Group();

            // Frame material placeholder (will be set by PreviewInstance)
            const frameMaterial = new THREE.MeshPhysicalMaterial({ color: 0x888888 });

            // Handle material
            const handleMaterial = new THREE.MeshStandardMaterial({
                color: 0xc0c0c0,
                roughness: 0.3,
                metalness: 0.9
            });

            const hw = width / 2;
            const hh = height / 2;
            const hd = depth / 2;

            // Helper to create frame/panel segment
            const createSegment = (w, h, d) => {
                return new THREE.BoxGeometry(w, h, d, 2, 2, 2);
            };

            // Main door panel (solid)
            const mainPanel = new THREE.Mesh(
                createSegment(width - frameWidth * 2, height - frameWidth * 2, depth - panelInset * 2),
                frameMaterial
            );
            mainPanel.position.set(0, 0, 0);
            mainPanel.userData.isFrame = true;
            group.add(mainPanel);

            // Top rail
            const topRail = new THREE.Mesh(
                createSegment(width, frameWidth, depth),
                frameMaterial
            );
            topRail.position.set(0, hh - frameWidth / 2, 0);
            topRail.userData.isFrame = true;
            group.add(topRail);

            // Bottom rail
            const bottomRail = new THREE.Mesh(
                createSegment(width, frameWidth, depth),
                frameMaterial
            );
            bottomRail.position.set(0, -hh + frameWidth / 2, 0);
            bottomRail.userData.isFrame = true;
            group.add(bottomRail);

            // Left stile
            const leftStile = new THREE.Mesh(
                createSegment(frameWidth, height - frameWidth * 2, depth),
                frameMaterial
            );
            leftStile.position.set(-hw + frameWidth / 2, 0, 0);
            leftStile.userData.isFrame = true;
            group.add(leftStile);

            // Right stile
            const rightStile = new THREE.Mesh(
                createSegment(frameWidth, height - frameWidth * 2, depth),
                frameMaterial
            );
            rightStile.position.set(hw - frameWidth / 2, 0, 0);
            rightStile.userData.isFrame = true;
            group.add(rightStile);

            // Door handle (lever style)
            const handleBaseRadius = 0.025;
            const handleBaseDepth = 0.015;
            const leverLength = 0.12;
            const leverRadius = 0.012;

            // Handle backplate
            const handleBackplate = new THREE.Mesh(
                new THREE.CylinderGeometry(handleBaseRadius * 1.5, handleBaseRadius * 1.5, handleBaseDepth, 16),
                handleMaterial
            );
            handleBackplate.rotation.x = Math.PI / 2;
            handleBackplate.position.set(hw - frameWidth - 0.08, 0, hd + handleBaseDepth / 2);
            group.add(handleBackplate);

            // Handle cylinder (rosette)
            const handleRosette = new THREE.Mesh(
                new THREE.CylinderGeometry(handleBaseRadius, handleBaseRadius, handleBaseDepth * 2, 16),
                handleMaterial
            );
            handleRosette.rotation.x = Math.PI / 2;
            handleRosette.position.set(hw - frameWidth - 0.08, 0, hd + handleBaseDepth * 1.5);
            group.add(handleRosette);

            // Lever handle
            const handleLever = new THREE.Mesh(
                new THREE.CapsuleGeometry(leverRadius, leverLength, 4, 8),
                handleMaterial
            );
            handleLever.rotation.z = Math.PI / 2;
            handleLever.position.set(hw - frameWidth - 0.08 - leverLength / 2 - leverRadius, 0, hd + handleBaseDepth * 2.5);
            group.add(handleLever);

            // Handle end (ball)
            const handleEnd = new THREE.Mesh(
                new THREE.SphereGeometry(leverRadius * 1.3, 12, 8),
                handleMaterial
            );
            handleEnd.position.set(hw - frameWidth - 0.08 - leverLength - leverRadius * 2, 0, hd + handleBaseDepth * 2.5);
            group.add(handleEnd);

            // Scale down to fit nicely in view
            group.scale.set(0.6, 0.6, 0.6);

            return group;
        }

        // Create S-curve sample geometry for material visualization
        static createCurvedSample(options = {}) {
            const {
                width = 1.2,
                height = 0.8,
                depth = 0.6,
                segmentsX = 64,
                segmentsY = 32,
                thickness = 0.04
            } = options;

            // Create parametric S-curve surface
            const geometry = new THREE.BufferGeometry();

            const verticesTop = [];
            const verticesBottom = [];
            const normals = [];
            const indices = [];

            // S-curve function: true S-shape with convex and concave regions
            const sCurve = (u) => {
                // Full sine wave creates proper S: up, cross zero, down
                // Amplitude 0.25 for a gentle curve
                return Math.sin(u * Math.PI * 2) * 0.25;
            };

            // Generate vertices for top surface
            for (let iy = 0; iy <= segmentsY; iy++) {
                const v = iy / segmentsY;
                for (let ix = 0; ix <= segmentsX; ix++) {
                    const u = ix / segmentsX;

                    // Position
                    const x = (u - 0.5) * width;
                    const y = (v - 0.5) * height;

                    // Z is the S-curve
                    const z = sCurve(u) * depth;

                    verticesTop.push(x, y, z + thickness / 2);
                    verticesBottom.push(x, y, z - thickness / 2);
                }
            }

            // Calculate normals for top surface
            const calcNormal = (vertices, ix, iy, cols) => {
                const idx = (iy * cols + ix) * 3;
                const idxRight = idx + 3;
                const idxUp = idx + cols * 3;

                // Get tangent vectors
                const tx = vertices[idxRight] - vertices[idx];
                const ty = vertices[idxRight + 1] - vertices[idx + 1];
                const tz = vertices[idxRight + 2] - vertices[idx + 2];

                const ux = vertices[idxUp] - vertices[idx];
                const uy = vertices[idxUp + 1] - vertices[idx + 1];
                const uz = vertices[idxUp + 2] - vertices[idx + 2];

                // Cross product
                let nx = ty * uz - tz * uy;
                let ny = tz * ux - tx * uz;
                let nz = tx * uy - ty * ux;

                // Normalize
                const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 0) {
                    nx /= len;
                    ny /= len;
                    nz /= len;
                }

                return [nx, ny, nz];
            };

            // Build complete mesh with top, bottom, and edges
            const allVertices = [];
            const allNormals = [];
            const allIndices = [];

            const cols = segmentsX + 1;
            const rows = segmentsY + 1;

            // Top surface
            const topOffset = 0;
            for (let iy = 0; iy < rows; iy++) {
                for (let ix = 0; ix < cols; ix++) {
                    const idx = (iy * cols + ix) * 3;
                    allVertices.push(verticesTop[idx], verticesTop[idx + 1], verticesTop[idx + 2]);

                    // Calculate normal (use finite differences)
                    const ixSafe = Math.min(ix, segmentsX - 1);
                    const iySafe = Math.min(iy, segmentsY - 1);
                    const [nx, ny, nz] = calcNormal(verticesTop, ixSafe, iySafe, cols);
                    allNormals.push(nx, ny, nz);
                }
            }

            // Generate indices for top surface
            for (let iy = 0; iy < segmentsY; iy++) {
                for (let ix = 0; ix < segmentsX; ix++) {
                    const a = topOffset + iy * cols + ix;
                    const b = topOffset + iy * cols + ix + 1;
                    const c = topOffset + (iy + 1) * cols + ix + 1;
                    const d = topOffset + (iy + 1) * cols + ix;
                    allIndices.push(a, b, c, a, c, d);
                }
            }

            // Bottom surface (reversed winding)
            const bottomOffset = allVertices.length / 3;
            for (let iy = 0; iy < rows; iy++) {
                for (let ix = 0; ix < cols; ix++) {
                    const idx = (iy * cols + ix) * 3;
                    allVertices.push(verticesBottom[idx], verticesBottom[idx + 1], verticesBottom[idx + 2]);

                    const ixSafe = Math.min(ix, segmentsX - 1);
                    const iySafe = Math.min(iy, segmentsY - 1);
                    const [nx, ny, nz] = calcNormal(verticesTop, ixSafe, iySafe, cols);
                    allNormals.push(-nx, -ny, -nz); // Flipped normal
                }
            }

            // Generate indices for bottom surface (reversed)
            for (let iy = 0; iy < segmentsY; iy++) {
                for (let ix = 0; ix < segmentsX; ix++) {
                    const a = bottomOffset + iy * cols + ix;
                    const b = bottomOffset + iy * cols + ix + 1;
                    const c = bottomOffset + (iy + 1) * cols + ix + 1;
                    const d = bottomOffset + (iy + 1) * cols + ix;
                    allIndices.push(a, c, b, a, d, c); // Reversed
                }
            }

            // Edge strips (connect top and bottom along perimeter)
            const addEdgeStrip = (startTop, startBottom, count, step, normalDir) => {
                const edgeOffset = allVertices.length / 3;

                for (let i = 0; i <= count; i++) {
                    const topIdx = (startTop + i * step) * 3;
                    const bottomIdx = (startBottom + i * step) * 3;

                    // Top edge vertex
                    allVertices.push(verticesTop[topIdx], verticesTop[topIdx + 1], verticesTop[topIdx + 2]);
                    allNormals.push(normalDir[0], normalDir[1], normalDir[2]);

                    // Bottom edge vertex
                    allVertices.push(verticesBottom[bottomIdx], verticesBottom[bottomIdx + 1], verticesBottom[bottomIdx + 2]);
                    allNormals.push(normalDir[0], normalDir[1], normalDir[2]);
                }

                // Generate indices for edge strip
                for (let i = 0; i < count; i++) {
                    const a = edgeOffset + i * 2;
                    const b = edgeOffset + i * 2 + 1;
                    const c = edgeOffset + (i + 1) * 2 + 1;
                    const d = edgeOffset + (i + 1) * 2;
                    allIndices.push(a, b, c, a, c, d);
                }
            };

            // Front edge (y = -height/2)
            addEdgeStrip(0, 0, segmentsX, 1, [0, -1, 0]);
            // Back edge (y = height/2)
            addEdgeStrip(segmentsY * cols, segmentsY * cols, segmentsX, 1, [0, 1, 0]);
            // Left edge (x = -width/2)
            addEdgeStrip(0, 0, segmentsY, cols, [-1, 0, 0]);
            // Right edge (x = width/2)
            addEdgeStrip(segmentsX, segmentsX, segmentsY, cols, [1, 0, 0]);

            geometry.setAttribute('position', new THREE.Float32BufferAttribute(allVertices, 3));
            geometry.setAttribute('normal', new THREE.Float32BufferAttribute(allNormals, 3));
            geometry.setIndex(allIndices);

            // Smooth the normals
            geometry.computeVertexNormals();

            return geometry;
        }
    }

    // ============================================
    // LIGHTING RIG
    // ============================================

    class LightingRig {
        constructor(scene) {
            this.scene = scene;
            this.lights = [];
            this.keyLight = null;
            this.fillLight = null;
        }

        createStudioSetup() {
            // Ambient light for overall base illumination
            const ambient = new THREE.AmbientLight(0xffffff, 0.3);
            this.lights.push(ambient);

            // Hemisphere light (sky/ground gradient)
            const hemi = new THREE.HemisphereLight(0xffffff, 0x888888, 0.35);
            hemi.position.set(0, 20, 0);
            this.lights.push(hemi);

            // Front light (main illumination from camera direction)
            const frontLight = new THREE.DirectionalLight(0xffffff, 0.9);
            frontLight.position.set(0, 2, 10);
            this.lights.push(frontLight);

            // Key light (main directional from top-right)
            this.keyLight = new THREE.DirectionalLight(0xffffff, 0.7);
            this.keyLight.position.set(5, 8, 5);
            this.lights.push(this.keyLight);

            // Fill light (left side, softer) - can be tinted by undertone
            this.fillLight = new THREE.DirectionalLight(0xffffff, 0.4);
            this.fillLight.position.set(-6, 4, 2);
            this.lights.push(this.fillLight);

            // Top light for specular highlights
            const topLight = new THREE.DirectionalLight(0xffffff, 0.3);
            topLight.position.set(0, 10, 0);
            this.lights.push(topLight);

            // Rim light (back light for edge definition)
            const rimLight = new THREE.DirectionalLight(0xffffff, 0.25);
            rimLight.position.set(0, 3, -8);
            this.lights.push(rimLight);

            // Add all lights to scene
            this.lights.forEach(light => this.scene.add(light));
        }

        // Apply undertone tint to fill light
        applyUndertone(undertoneHex, strength = 0.3) {
            if (!this.fillLight || !undertoneHex) return;

            // Parse undertone color
            const undertoneColor = new THREE.Color(undertoneHex);
            const white = new THREE.Color(0xffffff);

            // Blend white with undertone based on strength (subtle effect)
            const tintedColor = white.clone().lerp(undertoneColor, strength * 0.4);

            this.fillLight.color.copy(tintedColor);
        }

        // Adjust lighting based on LRV
        adjustForLrv(lrv) {
            // LRV 0-100: dark colors (low LRV) need more light, light colors need less
            // Map LRV to a multiplier: LRV 0 -> 1.8x, LRV 50 -> 1.0x, LRV 100 -> 0.7x
            const multiplier = 1.0 + (50 - lrv) / 60;
            const clampedMultiplier = Math.max(0.7, Math.min(1.8, multiplier));

            // Adjust all directional lights
            this.lights.forEach(light => {
                if (light.isDirectionalLight && light._baseIntensity === undefined) {
                    light._baseIntensity = light.intensity;
                }
                if (light.isDirectionalLight && light._baseIntensity !== undefined) {
                    light.intensity = light._baseIntensity * clampedMultiplier;
                }
            });

            // Store for later use
            this._lrvMultiplier = clampedMultiplier;
        }

        dispose() {
            this.lights.forEach(light => {
                this.scene.remove(light);
                if (light.dispose) light.dispose();
            });
            this.lights = [];
            this.keyLight = null;
        }
    }

    // ============================================
    // PREVIEW INSTANCE
    // ============================================

    class PreviewInstance {
        constructor(container, options) {
            this.container = typeof container === 'string'
                ? document.getElementById(container)
                : container;

            if (!this.container) {
                throw new Error('RalPreview: Container element not found');
            }

            this.options = this._mergeOptions(DEFAULT_OPTIONS, options);
            this.disposed = false;
            this.model = null;
            this.material = null;
            this.controls = null;
            this._autoRotate = false;
            this._animationId = null;

            this._init();
        }

        _mergeOptions(defaults, options) {
            const merged = { ...defaults };
            if (options) {
                Object.keys(options).forEach(key => {
                    if (typeof options[key] === 'object' && options[key] !== null && !Array.isArray(options[key])) {
                        merged[key] = { ...defaults[key], ...options[key] };
                    } else {
                        merged[key] = options[key];
                    }
                });
            }
            return merged;
        }

        _init() {
            // Create renderer
            const isTransparent = this.options.background === 'transparent';
            this.renderer = new THREE.WebGLRenderer({
                antialias: true,
                alpha: isTransparent,
                powerPreference: 'high-performance'
            });
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.renderer.toneMappingExposure = 1.1;
            this.renderer.outputColorSpace = THREE.SRGBColorSpace;

            const rect = this.container.getBoundingClientRect();
            const width = rect.width || 400;
            const height = rect.height || 300;
            this.renderer.setSize(width, height);
            this.container.appendChild(this.renderer.domElement);

            // Create scene
            this.scene = new THREE.Scene();
            if (!isTransparent) {
                this.scene.background = new THREE.Color(this.options.background);
            }

            // Create camera
            this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);

            // Setup lighting
            this.lightingRig = new LightingRig(this.scene);
            this.lightingRig.createStudioSetup();

            // Setup texture generator
            this.textureGen = new TextureGenerator();

            // Create model
            this._createModel();

            // Setup controls
            this._setupControls();

            // Frame camera to model
            this._frameCameraToModel();

            // Apply initial color
            this.setColor(this.options.color, { lrv: this.options.lrv });

            // Start render loop
            this._animate();

            // Handle resize
            this._resizeObserver = new ResizeObserver(() => this.resize());
            this._resizeObserver.observe(this.container);
        }

        _createModel() {
            const modelOption = this.options.model;

            if (modelOption && modelOption.object3D) {
                // External model provided
                this.model = modelOption.object3D.clone();
            } else if (modelOption === 'window-frame') {
                this.model = ModelFactory.createWindowFrame();
            } else if (modelOption === 'door') {
                this.model = ModelFactory.createDoor();
            } else if (modelOption === 'curved-sample') {
                const geometry = ModelFactory.createCurvedSample();
                this.material = this._createMaterial();
                this.model = new THREE.Mesh(geometry, this.material);
            } else {
                // Default: beveled cube
                const geometry = ModelFactory.createBeveledCube(0.85, 0.03, 10);
                this.material = this._createMaterial();
                this.model = new THREE.Mesh(geometry, this.material);
            }

            // Apply material to frame parts if it's a group
            if (this.model.isGroup) {
                this.material = this._createMaterial();
                this.model.traverse(child => {
                    if (child.isMesh && child.userData.isFrame) {
                        child.material = this.material;
                    }
                });
            }

            this.scene.add(this.model);
        }

        _createMaterial() {
            const preset = FINISH_PRESETS[this.options.finish] || FINISH_PRESETS.satin;
            const hex = this.options.color?.hex || '#888888';

            const material = new THREE.MeshPhysicalMaterial({
                color: new THREE.Color(hex),
                roughness: preset.roughness,
                metalness: preset.metalness,
                clearcoat: preset.clearcoat,
                clearcoatRoughness: preset.clearcoatRoughness,
                reflectivity: preset.reflectivity,
                envMapIntensity: preset.envMapIntensity
            });

            // Apply powder texture
            if (this.options.powder !== false) {
                const powderOpts = this.options.powder || {};
                material.roughnessMap = this.textureGen.createPowderRoughnessMap({
                    intensity: powderOpts.intensity || 0.3,
                    scale: powderOpts.scale || 1.0
                });
                material.normalMap = this.textureGen.createPowderNormalMap({
                    strength: powderOpts.intensity || 0.15,
                    scale: powderOpts.scale || 1.0
                });
                material.normalScale = new THREE.Vector2(0.08, 0.08);
            }

            return material;
        }

        _setupControls() {
            const controlsOpts = this.options.controls || {};

            // Check if OrbitControls is available
            const OrbitControls = THREE.OrbitControls ||
                (window.THREE && window.THREE.OrbitControls);

            if (OrbitControls && controlsOpts.orbit !== false) {
                this.controls = new OrbitControls(this.camera, this.renderer.domElement);
                this.controls.enableDamping = true;
                this.controls.dampingFactor = 0.05;
                this.controls.enableZoom = controlsOpts.zoom !== false;
                this.controls.enablePan = controlsOpts.pan === true;
                this.controls.autoRotate = controlsOpts.autoRotate === true;
                this.controls.autoRotateSpeed = 1.5;
                this.controls.minDistance = 0.5;
                this.controls.maxDistance = 5;
            } else if (controlsOpts.autoRotate) {
                // Fallback: simple auto-rotation without controls
                this._autoRotate = true;
            }
        }

        _frameCameraToModel() {
            // Calculate bounding box
            const box = new THREE.Box3().setFromObject(this.model);
            const size = box.getSize(new THREE.Vector3());
            const center = box.getCenter(new THREE.Vector3());

            // Calculate camera distance to fit model
            const maxDim = Math.max(size.x, size.y, size.z);
            const fov = this.camera.fov * (Math.PI / 180);
            const distance = maxDim / (2 * Math.tan(fov / 2)) * 2.2;

            // Position camera at an angle
            this.camera.position.set(
                center.x + distance * 0.5,
                center.y + distance * 0.35,
                center.z + distance * 0.8
            );
            this.camera.lookAt(center);

            // Update controls target if available
            if (this.controls) {
                this.controls.target.copy(center);
                this.controls.update();
            }
        }

        _animate() {
            if (this.disposed) return;

            this._animationId = requestAnimationFrame(() => this._animate());

            if (this.controls) {
                this.controls.update();
            } else if (this._autoRotate && this.model) {
                this.model.rotation.y += 0.008;
            }

            this.renderer.render(this.scene, this.camera);
        }

        // ========== PUBLIC API ==========

        setColor(colorData, options = {}) {
            const hex = colorData?.hex || colorData;
            const lrv = options.lrv;
            const undertoneHex = colorData?.undertoneHex;
            const undertoneStrength = colorData?.undertoneStrength ?? 0.3;

            if (this.material) {
                this.material.color.set(hex);
            }

            // Also update any frame materials in groups
            if (this.model && this.model.isGroup) {
                this.model.traverse(child => {
                    if (child.isMesh && child.userData.isFrame && child.material) {
                        child.material.color.set(hex);
                    }
                });
            }

            if (lrv !== undefined) {
                this.lightingRig.adjustForLrv(lrv);
            }

            // Apply undertone tint to fill light
            if (undertoneHex) {
                this.lightingRig.applyUndertone(undertoneHex, undertoneStrength);
            }
        }

        setFinish(finishName) {
            const preset = FINISH_PRESETS[finishName];
            if (!preset) return;

            const applyPreset = (material) => {
                if (material && material.isMeshPhysicalMaterial) {
                    material.roughness = preset.roughness;
                    material.metalness = preset.metalness;
                    material.clearcoat = preset.clearcoat;
                    material.clearcoatRoughness = preset.clearcoatRoughness;
                    material.reflectivity = preset.reflectivity;
                    material.envMapIntensity = preset.envMapIntensity;
                    material.needsUpdate = true;
                }
            };

            if (this.material) {
                applyPreset(this.material);
            }

            if (this.model && this.model.isGroup) {
                this.model.traverse(child => {
                    if (child.isMesh && child.userData.isFrame) {
                        applyPreset(child.material);
                    }
                });
            }
        }

        setPowder(options) {
            const applyTextures = (material) => {
                if (material && material.isMeshPhysicalMaterial) {
                    // Dispose old textures
                    if (material.roughnessMap) material.roughnessMap.dispose();
                    if (material.normalMap) material.normalMap.dispose();

                    // Create new textures
                    material.roughnessMap = this.textureGen.createPowderRoughnessMap(options);
                    material.normalMap = this.textureGen.createPowderNormalMap({
                        strength: options.intensity || 0.15,
                        scale: options.scale || 1.0
                    });
                    material.needsUpdate = true;
                }
            };

            if (this.material) {
                applyTextures(this.material);
            }

            if (this.model && this.model.isGroup) {
                this.model.traverse(child => {
                    if (child.isMesh && child.userData.isFrame) {
                        applyTextures(child.material);
                    }
                });
            }
        }

        setBackground(color) {
            if (color === 'transparent') {
                this.scene.background = null;
                this.renderer.setClearColor(0x000000, 0);
            } else {
                this.scene.background = new THREE.Color(color);
                this.renderer.setClearColor(color, 1);
            }
        }

        setAutoRotate(enabled) {
            if (this.controls) {
                this.controls.autoRotate = enabled;
            } else {
                this._autoRotate = enabled;
            }
        }

        resize() {
            if (this.disposed) return;

            const rect = this.container.getBoundingClientRect();
            const width = rect.width || 400;
            const height = rect.height || 300;

            this.camera.aspect = width / height;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(width, height);
        }

        screenshot() {
            return new Promise(resolve => {
                this.renderer.render(this.scene, this.camera);
                this.renderer.domElement.toBlob(blob => resolve(blob), 'image/png');
            });
        }

        dispose() {
            this.disposed = true;

            if (this._animationId) {
                cancelAnimationFrame(this._animationId);
            }

            if (this._resizeObserver) {
                this._resizeObserver.disconnect();
            }

            if (this.controls) {
                this.controls.dispose();
            }

            this.lightingRig.dispose();

            // Dispose model resources
            if (this.model) {
                this.model.traverse(child => {
                    if (child.isMesh) {
                        if (child.geometry) child.geometry.dispose();
                        if (child.material) {
                            if (child.material.map) child.material.map.dispose();
                            if (child.material.roughnessMap) child.material.roughnessMap.dispose();
                            if (child.material.normalMap) child.material.normalMap.dispose();
                            child.material.dispose();
                        }
                    }
                });
                this.scene.remove(this.model);
            }

            this.scene.clear();
            this.renderer.dispose();

            if (this.renderer.domElement && this.renderer.domElement.parentNode) {
                this.renderer.domElement.parentNode.removeChild(this.renderer.domElement);
            }
        }
    }

    // ============================================
    // PUBLIC API
    // ============================================

    window.RalPreview = {
        create: function (container, options) {
            return new PreviewInstance(container, options);
        },
        FINISHES: Object.keys(FINISH_PRESETS),
        MODELS: ['cube', 'window-frame', 'door', 'curved-sample']
    };

})();
