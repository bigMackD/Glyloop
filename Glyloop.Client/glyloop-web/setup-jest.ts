import { setupZoneTestEnv } from 'jest-preset-angular/setup-env/zone';
import '@angular/localize/init';

// Setup Zone.js for testing
setupZoneTestEnv();

// Mock window.matchMedia for Angular Material components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation((query) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  takeRecords() {
    return [];
  }
  unobserve() {}
} as any;

// Mock ResizeObserver
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  disconnect() {}
  observe() {}
  unobserve() {}
} as any;

// Suppress console errors/warnings in tests if needed
// global.console = {
//   ...console,
//   error: jest.fn(),
//   warn: jest.fn(),
// };

