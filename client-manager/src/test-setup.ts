import '@angular/compiler';
import { getTestBed } from '@angular/core/testing';
import {
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';

// Initialize Angular TestBed once per worker process.
// With pool:'forks', each spec file runs in its own process so this only
// runs once per file — no cross-test leakage.
getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting(),
);

// Clear localStorage before each test
beforeEach(() => {
  localStorage.clear();
});
