/**
 * Three.js Loader Module
 * Loads Three.js and OrbitControls, exposes them globally for ral-preview.js
 */
import * as THREE from '/lib/three-js/three.module.min.js';
import { OrbitControls } from '/lib/three-js/controls/OrbitControls.js';

// Create a mutable wrapper object with all THREE exports
const THREEWrapper = Object.assign({}, THREE);
THREEWrapper.OrbitControls = OrbitControls;

// Expose globally
window.THREE = THREEWrapper;

// Dispatch event when ready
window.dispatchEvent(new CustomEvent('three-ready'));
console.log('Three.js loaded:', THREE.REVISION);
