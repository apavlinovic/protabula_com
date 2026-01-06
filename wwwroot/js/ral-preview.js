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
        // Create window frame with realistic details
        static createWindowFrame(options = {}) {
            const {
                width = 1.0,
                height = 1.4,
                frameDepth = 0.075,
                frameWidth = 0.06,
                sashWidth = 0.045,
                sashDepth = 0.05
            } = options;

            const group = new THREE.Group();

            // Materials
            const frameMaterial = new THREE.MeshPhysicalMaterial({ color: 0x888888 });
            const glassMaterial = new THREE.MeshPhysicalMaterial({
                color: 0xffffff,
                transparent: true,
                opacity: 0.15,
                roughness: 0.05,
                metalness: 0.1,
                reflectivity: 0.9,
                clearcoat: 1.0,
                clearcoatRoughness: 0.05,
                side: THREE.DoubleSide,
                depthWrite: false
            });
            const gasketMaterial = new THREE.MeshStandardMaterial({
                color: 0x1a1a1a,
                roughness: 0.9,
                metalness: 0
            });
            const handleMaterial = new THREE.MeshStandardMaterial({
                color: 0xc0c0c0,
                roughness: 0.3,
                metalness: 0.9
            });

            // Helper: Create L-shaped frame profile and extrude along path
            const createFrameProfile = (profileWidth, profileDepth, stepDepth = 0.015) => {
                const shape = new THREE.Shape();
                // L-shaped profile with step for glass
                shape.moveTo(0, 0);
                shape.lineTo(profileWidth, 0);
                shape.lineTo(profileWidth, profileDepth - stepDepth);
                shape.lineTo(profileWidth - stepDepth, profileDepth - stepDepth);
                shape.lineTo(profileWidth - stepDepth, profileDepth);
                shape.lineTo(0, profileDepth);
                shape.lineTo(0, 0);
                return shape;
            };

            // Helper: Create extruded frame segment
            const createExtrudedSegment = (length, profileWidth, profileDepth, material) => {
                const shape = createFrameProfile(profileWidth, profileDepth);
                const geometry = new THREE.ExtrudeGeometry(shape, {
                    steps: 1,
                    depth: length,
                    bevelEnabled: false
                });
                geometry.rotateX(Math.PI / 2);
                geometry.translate(-profileWidth / 2, 0, 0);
                const mesh = new THREE.Mesh(geometry, material);
                mesh.userData.isFrame = true;
                return mesh;
            };

            // Helper: Create simple box segment
            const createBox = (w, h, d, material, isFrame = true) => {
                const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
                mesh.userData.isFrame = isFrame;
                return mesh;
            };

            const hw = width / 2;
            const hh = height / 2;

            // === OUTER FRAME ===
            // Using simple boxes for cleaner look, positioned to create frame

            // Top frame
            const topFrame = createBox(width, frameWidth, frameDepth, frameMaterial);
            topFrame.position.set(0, hh - frameWidth / 2, 0);
            group.add(topFrame);

            // Bottom frame (sill - slightly deeper)
            const bottomFrame = createBox(width + 0.02, frameWidth, frameDepth + 0.02, frameMaterial);
            bottomFrame.position.set(0, -hh + frameWidth / 2, 0.01);
            group.add(bottomFrame);

            // Left frame
            const leftFrame = createBox(frameWidth, height - frameWidth * 2, frameDepth, frameMaterial);
            leftFrame.position.set(-hw + frameWidth / 2, 0, 0);
            group.add(leftFrame);

            // Right frame
            const rightFrame = createBox(frameWidth, height - frameWidth * 2, frameDepth, frameMaterial);
            rightFrame.position.set(hw - frameWidth / 2, 0, 0);
            group.add(rightFrame);

            // === SASH (inner frame that holds glass) ===
            const sashInnerWidth = width - frameWidth * 2 - 0.008;
            const sashInnerHeight = height - frameWidth * 2 - 0.008;
            const sashOffset = frameDepth / 2 - sashDepth / 2 - 0.005;

            // Sash top
            const sashTop = createBox(sashInnerWidth, sashWidth, sashDepth, frameMaterial);
            sashTop.position.set(0, hh - frameWidth - sashWidth / 2 - 0.004, sashOffset);
            group.add(sashTop);

            // Sash bottom
            const sashBottom = createBox(sashInnerWidth, sashWidth, sashDepth, frameMaterial);
            sashBottom.position.set(0, -hh + frameWidth + sashWidth / 2 + 0.004, sashOffset);
            group.add(sashBottom);

            // Sash left
            const sashLeft = createBox(sashWidth, sashInnerHeight - sashWidth * 2, sashDepth, frameMaterial);
            sashLeft.position.set(-hw + frameWidth + sashWidth / 2 + 0.004, 0, sashOffset);
            group.add(sashLeft);

            // Sash right
            const sashRight = createBox(sashWidth, sashInnerHeight - sashWidth * 2, sashDepth, frameMaterial);
            sashRight.position.set(hw - frameWidth - sashWidth / 2 - 0.004, 0, sashOffset);
            group.add(sashRight);

            // Horizontal mullion (divider)
            const mullion = createBox(sashInnerWidth - sashWidth * 2, sashWidth * 0.7, sashDepth, frameMaterial);
            mullion.position.set(0, 0, sashOffset);
            group.add(mullion);

            // === GLASS PANES ===
            const glassWidth = sashInnerWidth - sashWidth * 2 - 0.01;
            const glassHeight = (sashInnerHeight - sashWidth * 2 - sashWidth * 0.7) / 2 - 0.005;
            const glassZ = sashOffset;

            // Upper glass
            const upperGlass = new THREE.Mesh(new THREE.PlaneGeometry(glassWidth, glassHeight), glassMaterial);
            upperGlass.position.set(0, glassHeight / 2 + sashWidth * 0.35 + 0.003, glassZ);
            group.add(upperGlass);

            // Lower glass
            const lowerGlass = new THREE.Mesh(new THREE.PlaneGeometry(glassWidth, glassHeight), glassMaterial);
            lowerGlass.position.set(0, -glassHeight / 2 - sashWidth * 0.35 - 0.003, glassZ);
            group.add(lowerGlass);

            // === GASKETS (rubber seals around glass) ===
            const gasketSize = 0.004;
            const createGasket = (w, h, x, y) => {
                const gasket = new THREE.Mesh(
                    new THREE.BoxGeometry(w, h, gasketSize),
                    gasketMaterial
                );
                gasket.position.set(x, y, glassZ + sashDepth / 2 - gasketSize);
                return gasket;
            };

            // Upper pane gaskets
            const uy = glassHeight / 2 + sashWidth * 0.35 + 0.003;
            group.add(createGasket(glassWidth + gasketSize * 2, gasketSize, 0, uy + glassHeight / 2));
            group.add(createGasket(glassWidth + gasketSize * 2, gasketSize, 0, uy - glassHeight / 2));
            group.add(createGasket(gasketSize, glassHeight, -glassWidth / 2 - gasketSize / 2, uy));
            group.add(createGasket(gasketSize, glassHeight, glassWidth / 2 + gasketSize / 2, uy));

            // Lower pane gaskets
            const ly = -glassHeight / 2 - sashWidth * 0.35 - 0.003;
            group.add(createGasket(glassWidth + gasketSize * 2, gasketSize, 0, ly + glassHeight / 2));
            group.add(createGasket(glassWidth + gasketSize * 2, gasketSize, 0, ly - glassHeight / 2));
            group.add(createGasket(gasketSize, glassHeight, -glassWidth / 2 - gasketSize / 2, ly));
            group.add(createGasket(gasketSize, glassHeight, glassWidth / 2 + gasketSize / 2, ly));

            // === WINDOW HANDLE ===
            const handleX = hw - frameWidth - sashWidth - 0.02;
            const handleY = -0.05;
            const handleZ = sashOffset + sashDepth / 2;

            // Handle base plate
            const basePlate = new THREE.Mesh(
                new THREE.BoxGeometry(0.025, 0.08, 0.008),
                handleMaterial
            );
            basePlate.position.set(handleX, handleY, handleZ + 0.004);
            group.add(basePlate);

            // Handle lever
            const lever = new THREE.Mesh(
                new THREE.BoxGeometry(0.012, 0.06, 0.015),
                handleMaterial
            );
            lever.position.set(handleX, handleY + 0.04, handleZ + 0.012);
            group.add(lever);

            // Handle grip (rotated down for closed position)
            const grip = new THREE.Mesh(
                new THREE.CapsuleGeometry(0.008, 0.05, 4, 8),
                handleMaterial
            );
            grip.rotation.x = Math.PI / 2;
            grip.position.set(handleX, handleY + 0.07, handleZ + 0.035);
            group.add(grip);

            // === DRAINAGE SLOTS (bottom frame detail) ===
            const slotMaterial = new THREE.MeshStandardMaterial({ color: 0x333333, roughness: 0.8 });
            for (let i = -1; i <= 1; i += 2) {
                const slot = new THREE.Mesh(
                    new THREE.BoxGeometry(0.025, 0.004, 0.01),
                    slotMaterial
                );
                slot.position.set(i * 0.15, -hh + frameWidth / 2, frameDepth / 2 + 0.005);
                group.add(slot);
            }

            return group;
        }

        // Create modern door with vertical glass slot
        static createDoor(options = {}) {
            const {
                width = 1.0,
                height = 2.1,
                doorDepth = 0.055,
                frameWidth = 0.065,
                frameDepth = 0.12
            } = options;

            const group = new THREE.Group();

            // === MATERIALS ===
            const frameMaterial = new THREE.MeshPhysicalMaterial({ color: 0x888888 });
            const glassMaterial = new THREE.MeshPhysicalMaterial({
                color: 0x88bbdd,
                transparent: true,
                opacity: 0.25,
                roughness: 0.02,
                metalness: 0.05,
                clearcoat: 0.8,
                clearcoatRoughness: 0.1,
                side: THREE.DoubleSide,
                depthWrite: false
            });
            const handleMaterial = new THREE.MeshStandardMaterial({
                color: 0x303030,
                roughness: 0.3,
                metalness: 0.9
            });
            const gasketMaterial = new THREE.MeshStandardMaterial({
                color: 0x1a1a1a,
                roughness: 0.9,
                metalness: 0
            });
            const thresholdMaterial = new THREE.MeshStandardMaterial({
                color: 0x606060,
                roughness: 0.4,
                metalness: 0.85
            });

            // Helper: Create box mesh
            const createBox = (w, h, d, material, isFrame = true) => {
                const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), material);
                mesh.userData.isFrame = isFrame;
                return mesh;
            };

            const hw = width / 2;
            const hh = height / 2;
            const doorOffset = frameDepth / 2 - doorDepth / 2 - 0.02;

            // === DOOR FRAME (fixed part) ===
            // Top frame
            const topFrame = createBox(width + frameWidth * 2, frameWidth, frameDepth, frameMaterial);
            topFrame.position.set(0, hh + frameWidth / 2, 0);
            group.add(topFrame);

            // Left frame
            const leftFrame = createBox(frameWidth, height, frameDepth, frameMaterial);
            leftFrame.position.set(-hw - frameWidth / 2, 0, 0);
            group.add(leftFrame);

            // Right frame
            const rightFrame = createBox(frameWidth, height, frameDepth, frameMaterial);
            rightFrame.position.set(hw + frameWidth / 2, 0, 0);
            group.add(rightFrame);

            // === THRESHOLD (bottom) ===
            const threshold = createBox(width + frameWidth * 2 + 0.02, 0.025, frameDepth + 0.04, thresholdMaterial, false);
            threshold.position.set(0, -hh - 0.0125, 0.02);
            group.add(threshold);

            // === DOOR LEAF (modern flat panel) ===
            const leafWidth = width - 0.006;
            const leafHeight = height - 0.008;

            // Main door panel (flush, modern look)
            const doorPanel = createBox(leafWidth, leafHeight, doorDepth, frameMaterial);
            doorPanel.position.set(0, 0, doorOffset);
            group.add(doorPanel);

            // === VERTICAL GLASS SLOT ===
            const glassSlotWidth = 0.12;
            const glassSlotHeight = leafHeight * 0.7;
            const glassSlotX = hw - 0.22;
            const glassSlotY = leafHeight * 0.08;
            const glassInset = 0.012;

            // Cut-out frame around glass (creates depth illusion)
            // Left edge
            const glassFrameLeft = createBox(glassInset, glassSlotHeight, doorDepth + 0.002, frameMaterial);
            glassFrameLeft.position.set(glassSlotX - glassSlotWidth / 2 - glassInset / 2, glassSlotY, doorOffset);
            group.add(glassFrameLeft);

            // Right edge
            const glassFrameRight = createBox(glassInset, glassSlotHeight, doorDepth + 0.002, frameMaterial);
            glassFrameRight.position.set(glassSlotX + glassSlotWidth / 2 + glassInset / 2, glassSlotY, doorOffset);
            group.add(glassFrameRight);

            // Top edge
            const glassFrameTop = createBox(glassSlotWidth + glassInset * 2, glassInset, doorDepth + 0.002, frameMaterial);
            glassFrameTop.position.set(glassSlotX, glassSlotY + glassSlotHeight / 2 + glassInset / 2, doorOffset);
            group.add(glassFrameTop);

            // Bottom edge
            const glassFrameBottom = createBox(glassSlotWidth + glassInset * 2, glassInset, doorDepth + 0.002, frameMaterial);
            glassFrameBottom.position.set(glassSlotX, glassSlotY - glassSlotHeight / 2 - glassInset / 2, doorOffset);
            group.add(glassFrameBottom);

            // Glass pane (set back slightly)
            const glass = new THREE.Mesh(
                new THREE.PlaneGeometry(glassSlotWidth, glassSlotHeight),
                glassMaterial
            );
            glass.position.set(glassSlotX, glassSlotY, doorOffset - doorDepth / 2 + 0.008);
            group.add(glass);

            // Inner glass gasket
            const gasketWidth = 0.003;
            const innerGasketTop = createBox(glassSlotWidth, gasketWidth, gasketWidth, gasketMaterial, false);
            innerGasketTop.position.set(glassSlotX, glassSlotY + glassSlotHeight / 2 - gasketWidth / 2, doorOffset + doorDepth / 2 + 0.001);
            group.add(innerGasketTop);

            const innerGasketBottom = createBox(glassSlotWidth, gasketWidth, gasketWidth, gasketMaterial, false);
            innerGasketBottom.position.set(glassSlotX, glassSlotY - glassSlotHeight / 2 + gasketWidth / 2, doorOffset + doorDepth / 2 + 0.001);
            group.add(innerGasketBottom);

            const innerGasketLeft = createBox(gasketWidth, glassSlotHeight - gasketWidth * 2, gasketWidth, gasketMaterial, false);
            innerGasketLeft.position.set(glassSlotX - glassSlotWidth / 2 + gasketWidth / 2, glassSlotY, doorOffset + doorDepth / 2 + 0.001);
            group.add(innerGasketLeft);

            const innerGasketRight = createBox(gasketWidth, glassSlotHeight - gasketWidth * 2, gasketWidth, gasketMaterial, false);
            innerGasketRight.position.set(glassSlotX + glassSlotWidth / 2 - gasketWidth / 2, glassSlotY, doorOffset + doorDepth / 2 + 0.001);
            group.add(innerGasketRight);

            // === MODERN HANDLE (long vertical bar) ===
            const handleX = -hw + 0.12;
            const handleY = 0;
            const handleZ = doorOffset + doorDepth / 2;
            const handleLength = 0.35;

            // Mounting plates (top and bottom)
            const mountPlate1 = new THREE.Mesh(
                new THREE.CylinderGeometry(0.018, 0.018, 0.012, 16),
                handleMaterial
            );
            mountPlate1.rotation.x = Math.PI / 2;
            mountPlate1.position.set(handleX, handleY + handleLength / 2 - 0.03, handleZ + 0.006);
            group.add(mountPlate1);

            const mountPlate2 = new THREE.Mesh(
                new THREE.CylinderGeometry(0.018, 0.018, 0.012, 16),
                handleMaterial
            );
            mountPlate2.rotation.x = Math.PI / 2;
            mountPlate2.position.set(handleX, handleY - handleLength / 2 + 0.03, handleZ + 0.006);
            group.add(mountPlate2);

            // Main handle bar (vertical)
            const handleBar = new THREE.Mesh(
                new THREE.CapsuleGeometry(0.012, handleLength - 0.06, 8, 16),
                handleMaterial
            );
            handleBar.position.set(handleX, handleY, handleZ + 0.035);
            group.add(handleBar);

            // Handle standoffs
            const standoff1 = new THREE.Mesh(
                new THREE.CylinderGeometry(0.008, 0.008, 0.025, 8),
                handleMaterial
            );
            standoff1.rotation.x = Math.PI / 2;
            standoff1.position.set(handleX, handleY + handleLength / 2 - 0.03, handleZ + 0.02);
            group.add(standoff1);

            const standoff2 = new THREE.Mesh(
                new THREE.CylinderGeometry(0.008, 0.008, 0.025, 8),
                handleMaterial
            );
            standoff2.rotation.x = Math.PI / 2;
            standoff2.position.set(handleX, handleY - handleLength / 2 + 0.03, handleZ + 0.02);
            group.add(standoff2);

            // === LOCK (separate from handle, modern style) ===
            const lockX = -hw + 0.12;
            const lockY = -0.35;

            // Lock cylinder housing
            const lockHousing = new THREE.Mesh(
                new THREE.CylinderGeometry(0.016, 0.016, 0.015, 16),
                handleMaterial
            );
            lockHousing.rotation.x = Math.PI / 2;
            lockHousing.position.set(lockX, lockY, handleZ + 0.0075);
            group.add(lockHousing);

            // Lock cylinder
            const lockCylinder = new THREE.Mesh(
                new THREE.CylinderGeometry(0.01, 0.01, 0.008, 16),
                thresholdMaterial
            );
            lockCylinder.rotation.x = Math.PI / 2;
            lockCylinder.position.set(lockX, lockY, handleZ + 0.018);
            group.add(lockCylinder);

            // === HINGES (concealed style - just visible barrels) ===
            const hingeX = hw - 0.02;
            const hingePositions = [hh - 0.2, hh - 0.6, -hh + 0.2];

            hingePositions.forEach(y => {
                const hinge = new THREE.Mesh(
                    new THREE.CylinderGeometry(0.006, 0.006, 0.06, 8),
                    handleMaterial
                );
                hinge.position.set(hingeX, y, doorOffset);
                group.add(hinge);
            });

            // === WEATHER STRIPPING ===
            const gasketSize = 0.005;

            const topGasket = createBox(leafWidth, gasketSize, gasketSize, gasketMaterial, false);
            topGasket.position.set(0, hh - 0.006, doorOffset - doorDepth / 2 - gasketSize / 2);
            group.add(topGasket);

            const leftGasket = createBox(gasketSize, leafHeight - 0.02, gasketSize, gasketMaterial, false);
            leftGasket.position.set(-leafWidth / 2 + 0.003, 0, doorOffset - doorDepth / 2 - gasketSize / 2);
            group.add(leftGasket);

            const rightGasket = createBox(gasketSize, leafHeight - 0.02, gasketSize, gasketMaterial, false);
            rightGasket.position.set(leafWidth / 2 - 0.003, 0, doorOffset - doorDepth / 2 - gasketSize / 2);
            group.add(rightGasket);

            // Scale to fit nicely
            group.scale.set(0.52, 0.52, 0.52);

            return group;
        }

        // Create sphere geometry
        static createSphere(radius = 0.7, segments = 64) {
            return new THREE.SphereGeometry(radius, segments, segments);
        }

        // Create bent panel geometry - folded sheet metal (L-shape)
        static createBentPanel(options = {}) {
            const {
                width = 1.2,
                legLength = 0.9,
                thickness = 0.03,
                bendRadius = 0.08,
                bendSegments = 48
            } = options;

            // Use Three.js Shape and ExtrudeGeometry for a clean, watertight mesh
            const shape = new THREE.Shape();

            const outerR = bendRadius + thickness / 2;
            const innerR = bendRadius - thickness / 2;

            // Both legs should have equal visible length and thickness
            // Arc center positioned so inner/outer arcs align with leg surfaces

            const arcCenterX = legLength;
            const arcCenterY = thickness / 2 + innerR; // Places inner arc tangent to horizontal leg top

            // Start at bottom-left of horizontal leg (outer edge)
            shape.moveTo(0, -thickness / 2);

            // Bottom edge of horizontal leg
            shape.lineTo(legLength, -thickness / 2);

            // Outer arc of bend
            for (let i = 0; i <= bendSegments; i++) {
                const angle = -Math.PI / 2 + (Math.PI / 2) * (i / bendSegments);
                const x = arcCenterX + Math.cos(angle) * outerR;
                const y = arcCenterY + Math.sin(angle) * outerR;
                shape.lineTo(x, y);
            }

            // Right edge of vertical leg (going up) - same length as horizontal
            const verticalEndY = arcCenterY + legLength;
            shape.lineTo(legLength + outerR, verticalEndY);

            // Top edge of vertical leg
            shape.lineTo(legLength + innerR, verticalEndY);

            // Inner edge of vertical leg (going down to arc)
            shape.lineTo(legLength + innerR, arcCenterY);

            // Inner arc of bend
            for (let i = bendSegments; i >= 0; i--) {
                const angle = -Math.PI / 2 + (Math.PI / 2) * (i / bendSegments);
                const x = arcCenterX + Math.cos(angle) * innerR;
                const y = arcCenterY + Math.sin(angle) * innerR;
                shape.lineTo(x, y);
            }

            // Top edge of horizontal leg (going back to start)
            shape.lineTo(0, thickness / 2);

            // Left edge (close the shape)
            shape.lineTo(0, -thickness / 2);

            // Extrude the shape to give it depth (width)
            const extrudeSettings = {
                steps: 1,
                depth: width,
                bevelEnabled: false
            };

            const geometry = new THREE.ExtrudeGeometry(shape, extrudeSettings);

            // Center the geometry
            geometry.translate(-legLength / 2, -legLength / 2, -width / 2);

            // Rotate so the L-shape is oriented nicely
            geometry.rotateX(-Math.PI / 2);

            geometry.computeVertexNormals();

            return geometry;
        }

        // Create S-curve sample geometry for material visualization
        static createCurvedSample(options = {}) {
            const {
                width = 1.0,
                amplitude = 0.15,
                thickness = 0.03,
                segments = 192
            } = options;

            // Use Three.js Shape and ExtrudeGeometry for a clean, watertight mesh
            // The S-curve is defined in 2D (X = position along curve, Y = height based on sine)
            const shape = new THREE.Shape();

            // S-curve function: sine wave
            const sCurve = (u) => Math.sin(u * Math.PI * 2) * amplitude;

            // Generate points along the S-curve for outer edge (top surface)
            const outerPoints = [];
            const innerPoints = [];

            for (let i = 0; i <= segments; i++) {
                const u = i / segments;
                const x = (u - 0.5) * width;
                const y = sCurve(u);
                outerPoints.push({ x, y: y + thickness / 2 });
                innerPoints.push({ x, y: y - thickness / 2 });
            }

            // Start at the left end of outer edge
            shape.moveTo(outerPoints[0].x, outerPoints[0].y);

            // Trace outer edge (top) left to right
            for (let i = 1; i <= segments; i++) {
                shape.lineTo(outerPoints[i].x, outerPoints[i].y);
            }

            // Right end cap - connect outer to inner
            shape.lineTo(innerPoints[segments].x, innerPoints[segments].y);

            // Trace inner edge (bottom) right to left
            for (let i = segments - 1; i >= 0; i--) {
                shape.lineTo(innerPoints[i].x, innerPoints[i].y);
            }

            // Left end cap - close the shape
            shape.lineTo(outerPoints[0].x, outerPoints[0].y);

            // Extrude the shape to give it depth
            const extrudeSettings = {
                steps: 64,
                depth: width * 0.7,
                bevelEnabled: false
            };

            const geometry = new THREE.ExtrudeGeometry(shape, extrudeSettings);

            // Center the geometry
            geometry.translate(0, 0, -width * 0.35);

            // Rotate so the S-curve is horizontal with waves going up/down
            geometry.rotateX(Math.PI / 2);

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
            this.rimLight = null;
            this._animating = false;
            this._animationId = null;
        }

        createStudioSetup() {
            // Classic 3-point lighting setup for product visualization
            // Camera is at front-right-top, ~30° from front

            // Ambient: Soft hemisphere for base illumination (prevents pure black shadows)
            const hemi = new THREE.HemisphereLight(0xffffff, 0xe8e8e8, 0.8);
            hemi.position.set(0, 10, 0);
            this.lights.push(hemi);

            // Key light: Main light, 45° right of camera, 45° above
            // Strongest light, defines the main illumination direction
            this.keyLight = new THREE.DirectionalLight(0xffffff, 1.2);
            this.keyLight.position.set(5, 5, 5);
            this.lights.push(this.keyLight);

            // Fill light: Opposite side of key, softer (about 1/2 key intensity)
            // Fills in shadows without eliminating them
            this.fillLight = new THREE.DirectionalLight(0xffffff, 0.6);
            this.fillLight.position.set(-4, 3, 4);
            this.lights.push(this.fillLight);

            // Rim/back light: Behind and above, creates edge definition
            // Separates object from background
            this.rimLight = new THREE.DirectionalLight(0xffffff, 0.5);
            this.rimLight.position.set(0, 4, -4);
            this.lights.push(this.rimLight);

            // Add all lights to scene
            this.lights.forEach(light => this.scene.add(light));
        }

        // Apply undertone tint to fill light
        applyUndertone(undertoneHex, strength = 0.3) {
            if (!this.fillLight || !undertoneHex) return;

            // Parse undertone color
            const undertoneColor = new THREE.Color(undertoneHex);
            const white = new THREE.Color(0xffffff);

            // Blend white with undertone based on strength (visible under light)
            const tintedColor = white.clone().lerp(undertoneColor, strength * 0.6);

            this.fillLight.color.copy(tintedColor);
        }

        // Lighting presets
        static PRESETS = {
            'studio': {
                hemi: { skyColor: 0xffffff, groundColor: 0xf0f0f0, intensity: 0.8 },
                key: { color: 0xffffff, intensity: 1.2 },
                fill: { color: 0xffffff, intensity: 0.6 },
                rim: { color: 0xffffff, intensity: 0.5 }
            },
            'morning': {
                hemi: { skyColor: 0xfff8f0, groundColor: 0xf0ebe5, intensity: 0.75 },
                key: { color: 0xffe8d0, intensity: 1.2 },
                fill: { color: 0xe8f0f8, intensity: 0.55 },
                rim: { color: 0xfff4e8, intensity: 0.45 }
            },
            'midday': {
                hemi: { skyColor: 0xfcfcff, groundColor: 0xf0f0f0, intensity: 0.85 },
                key: { color: 0xfffcf8, intensity: 1.25 },
                fill: { color: 0xf0f4f8, intensity: 0.6 },
                rim: { color: 0xffffff, intensity: 0.5 }
            },
            'golden': {
                hemi: { skyColor: 0xfff0e0, groundColor: 0xe8ddd0, intensity: 0.7 },
                key: { color: 0xffd8a8, intensity: 1.15 },
                fill: { color: 0xd8e0e8, intensity: 0.5 },
                rim: { color: 0xffe8c8, intensity: 0.4 }
            },
            'overcast': {
                hemi: { skyColor: 0xf4f4f8, groundColor: 0xe8e8e8, intensity: 0.9 },
                key: { color: 0xf8f8fc, intensity: 1.0 },
                fill: { color: 0xf0f2f6, intensity: 0.65 },
                rim: { color: 0xf0f0f4, intensity: 0.4 }
            },
            'night': {
                // Cool moonlight - visible but with blue tint
                hemi: { skyColor: 0x445577, groundColor: 0x202030, intensity: 0.5 },
                key: { color: 0x99aacc, intensity: 0.9 },
                fill: { color: 0x6677aa, intensity: 0.5 },
                rim: { color: 0xaabbdd, intensity: 0.5 }
            }
        };

        // Apply lighting preset with smooth animation
        applyPreset(preset, duration = 600) {
            const target = LightingRig.PRESETS[preset] || LightingRig.PRESETS['studio'];

            // Cancel any ongoing animation
            if (this._animationId) {
                cancelAnimationFrame(this._animationId);
            }

            // Capture current state
            const hemi = this.lights[0];
            const start = {
                hemi: {
                    skyColor: hemi ? hemi.color.clone() : new THREE.Color(0xffffff),
                    groundColor: hemi ? hemi.groundColor.clone() : new THREE.Color(0xf0f0f0),
                    intensity: hemi ? hemi.intensity : 0.8
                },
                key: {
                    color: this.keyLight ? this.keyLight.color.clone() : new THREE.Color(0xffffff),
                    intensity: this.keyLight ? this.keyLight.intensity : 1.2
                },
                fill: {
                    color: this.fillLight ? this.fillLight.color.clone() : new THREE.Color(0xffffff),
                    intensity: this.fillLight ? this.fillLight.intensity : 0.6
                },
                rim: {
                    color: this.rimLight ? this.rimLight.color.clone() : new THREE.Color(0xffffff),
                    intensity: this.rimLight ? this.rimLight.intensity : 0.5
                }
            };

            // Target colors as THREE.Color objects
            const targetColors = {
                hemi: {
                    skyColor: new THREE.Color(target.hemi.skyColor),
                    groundColor: new THREE.Color(target.hemi.groundColor)
                },
                key: { color: new THREE.Color(target.key.color) },
                fill: { color: new THREE.Color(target.fill.color) },
                rim: { color: new THREE.Color(target.rim.color) }
            };

            const startTime = performance.now();

            const animate = (currentTime) => {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);

                // Ease out cubic for smooth deceleration
                const eased = 1 - Math.pow(1 - progress, 3);

                // Interpolate hemisphere light
                if (hemi) {
                    hemi.color.lerpColors(start.hemi.skyColor, targetColors.hemi.skyColor, eased);
                    hemi.groundColor.lerpColors(start.hemi.groundColor, targetColors.hemi.groundColor, eased);
                    hemi.intensity = start.hemi.intensity + (target.hemi.intensity - start.hemi.intensity) * eased;
                }

                // Interpolate key light
                if (this.keyLight) {
                    this.keyLight.color.lerpColors(start.key.color, targetColors.key.color, eased);
                    this.keyLight.intensity = start.key.intensity + (target.key.intensity - start.key.intensity) * eased;
                }

                // Interpolate fill light
                if (this.fillLight) {
                    this.fillLight.color.lerpColors(start.fill.color, targetColors.fill.color, eased);
                    this.fillLight.intensity = start.fill.intensity + (target.fill.intensity - start.fill.intensity) * eased;
                }

                // Interpolate rim light
                if (this.rimLight) {
                    this.rimLight.color.lerpColors(start.rim.color, targetColors.rim.color, eased);
                    this.rimLight.intensity = start.rim.intensity + (target.rim.intensity - start.rim.intensity) * eased;
                }

                if (progress < 1) {
                    this._animationId = requestAnimationFrame(animate);
                } else {
                    this._animationId = null;
                }
            };

            this._animationId = requestAnimationFrame(animate);
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
            // Cancel any ongoing animation
            if (this._animationId) {
                cancelAnimationFrame(this._animationId);
                this._animationId = null;
            }

            this.lights.forEach(light => {
                this.scene.remove(light);
                if (light.dispose) light.dispose();
            });
            this.lights = [];
            this.keyLight = null;
            this.fillLight = null;
            this.rimLight = null;
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
                alpha: true,
                powerPreference: 'high-performance'
            });
            this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
            this.renderer.setClearColor(0x000000, 0); // Fully transparent
            this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
            this.renderer.toneMappingExposure = 1.1;
            this.renderer.outputColorSpace = THREE.SRGBColorSpace;

            const rect = this.container.getBoundingClientRect();
            const width = rect.width || 400;
            const height = rect.height || 300;
            this.renderer.setSize(width, height, false);
            this.container.appendChild(this.renderer.domElement);

            // Create scene with gradient background
            this.scene = new THREE.Scene();
            this.scene.background = this._createGradientTexture();

            // Create camera
            this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);

            // Setup lighting
            this.lightingRig = new LightingRig(this.scene);
            this.lightingRig.createStudioSetup();

            // Add floor grid
            this._createGrid();

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
            } else if (modelOption === 'bent-panel') {
                const geometry = ModelFactory.createBentPanel();
                this.material = this._createMaterial();
                this.model = new THREE.Mesh(geometry, this.material);
            } else {
                // Default: sphere (best for showing material properties)
                const geometry = ModelFactory.createSphere(0.5, 64);
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

            // Tilt model toward camera (-20°)
            this.model.rotation.x = -0.35;

            this.scene.add(this.model);
        }

        _createGradientTexture() {
            const canvas = document.createElement('canvas');
            canvas.width = 2;
            canvas.height = 512;
            const ctx = canvas.getContext('2d');

            // Darker gradient: medium gray at top to darker gray at bottom
            const gradient = ctx.createLinearGradient(0, 0, 0, 512);
            gradient.addColorStop(0, '#606060');
            gradient.addColorStop(1, '#303030');

            ctx.fillStyle = gradient;
            ctx.fillRect(0, 0, 2, 512);

            const texture = new THREE.CanvasTexture(canvas);
            texture.needsUpdate = true;
            return texture;
        }

        _createGrid() {
            // Create a subtle floor grid for visual grounding
            const gridSize = 4;
            const gridDivisions = 16;

            // Grid colors - lighter for dark background
            const gridColor = 0x555555;
            const gridCenterColor = 0x666666;

            this.grid = new THREE.GridHelper(gridSize, gridDivisions, gridCenterColor, gridColor);
            this.grid.position.y = -0.8;
            this.grid.material.opacity = 0.5;
            this.grid.material.transparent = true;

            this.scene.add(this.grid);
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

            // Calculate camera distance to fit model (lower = more zoomed in)
            const maxDim = Math.max(size.x, size.y, size.z);
            const fov = this.camera.fov * (Math.PI / 180);
            const distance = maxDim / (2 * Math.tan(fov / 2)) * 1.6;

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

        setLighting(presetName) {
            if (this.lightingRig) {
                this.lightingRig.applyPreset(presetName);
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
            this.renderer.setSize(width, height, false);
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

            // Dispose grid
            if (this.grid) {
                this.scene.remove(this.grid);
                if (this.grid.geometry) this.grid.geometry.dispose();
                if (this.grid.material) this.grid.material.dispose();
            }

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
        MODELS: ['sphere', 'bent-panel', 'curved-sample', 'window-frame', 'door']
    };

})();
