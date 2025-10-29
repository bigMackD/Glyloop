/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./index.html","./src/**/*.{html,ts,scss}"],
  theme: {
    extend: {
      colors: {
        // Dark Theme Colors
        'app-bg': '#111827', // gray-900 to match login page
        'card-bg': '#1f2937', // gray-800 for cards
        'card-border': '#374151', // gray-700 for borders
        
        // Primary/Accent colors with gradient
        primary: {
          DEFAULT: '#5B8DEF',
          hover: '#4a7dd8',
          light: '#7ba5f4',
          dark: '#3d6bc7',
          from: '#5B8DEF',
          to: '#06b6d4',
        },
        
        // Glucose range colors
        'glucose-in-range': {
          DEFAULT: '#10b981',
          bg: '#064e3b',
          border: '#059669',
        },
        'glucose-below-range': {
          DEFAULT: '#f59e0b',
          bg: '#78350f',
          border: '#d97706',
        },
        'glucose-above-range': {
          DEFAULT: '#ef4444',
          bg: '#7f1d1d',
          border: '#dc2626',
        },
        
        // Text colors
        'text-primary': '#e5e7eb',
        'text-secondary': '#9ca3af',
        'text-muted': '#6b7280',
        
        // Material 3 Color System (keep for compatibility)
        secondary: {
          DEFAULT: 'var(--mat-sys-secondary, #625B71)',
          container: 'var(--mat-sys-secondary-container, #E8DEF8)',
        },
        tertiary: {
          DEFAULT: 'var(--mat-sys-tertiary, #7D5260)',
          container: 'var(--mat-sys-tertiary-container, #FFD8E4)',
        },
        error: {
          DEFAULT: '#ef4444',
          container: '#7f1d1d',
        },
        success: {
          DEFAULT: '#10b981',
          container: '#064e3b',
        },
        background: '#111827',
        surface: {
          DEFAULT: '#1f2937',
          variant: '#374151',
          container: {
            DEFAULT: '#1f2937',
            high: '#374151',
            highest: '#4b5563',
          },
        },
        outline: {
          DEFAULT: '#4b5563',
          variant: '#374151',
        },
        'on-primary': '#ffffff',
        'on-primary-container': '#e5e7eb',
        'on-secondary': '#ffffff',
        'on-secondary-container': '#e5e7eb',
        'on-tertiary': '#ffffff',
        'on-tertiary-container': '#e5e7eb',
        'on-error': '#ffffff',
        'on-error-container': '#fca5a5',
        'on-success-container': '#6ee7b7',
        'on-background': '#e5e7eb',
        'on-surface': '#e5e7eb',
        'on-surface-variant': '#9ca3af',
      },
    },
  },
  plugins: [],
}

