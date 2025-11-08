/**
 * Sample Jest unit test for Angular components
 * Demonstrates best practices following jest-unit-testing.mdc guidelines
 * 
 * Key principles:
 * - Use Jest with TypeScript for type checking
 * - Implement Testing Library for component testing
 * - Use describe blocks for organizing related tests
 * - Leverage expect assertions with specific matchers
 * - Use beforeEach and afterEach for setup and teardown
 */

import { waitFor } from '@testing-library/angular';
import '@testing-library/jest-dom';

// Mock component for demonstration
class SampleComponent {
  title = 'Sample Component';
  count = 0;
  loading = false;

  increment(): void {
    this.count++;
  }

  async fetchData(): Promise<void> {
    this.loading = true;
    // Simulate async operation
    await new Promise(resolve => setTimeout(resolve, 100));
    this.loading = false;
  }
}

describe('SampleComponent', () => {
  let component: SampleComponent;

  /**
   * Setup: Use beforeEach for test initialization
   */
  beforeEach(async () => {
    // Reset component state before each test
    component = new SampleComponent();
  });

  /**
   * Cleanup: Use afterEach for teardown
   */
  afterEach(() => {
    // Clean up any subscriptions, timers, etc.
    jest.clearAllMocks();
  });

  /**
   * Grouping related tests with describe blocks
   */
  describe('Component Initialization', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
      // Arrange & Act (done in beforeEach)
      
      // Assert
      expect(component.title).toBe('Sample Component');
      expect(component.count).toBe(0);
      expect(component.loading).toBe(false);
    });

    it('should have correct initial state using multiple assertions', () => {
      // Using multiple specific matchers
      expect(component.title).toBeDefined();
      expect(component.title).toMatch(/Sample/);
      expect(component.count).toBeGreaterThanOrEqual(0);
      expect(component.loading).toBeFalsy();
    });
  });

  describe('Counter Functionality', () => {
    it('should increment count when increment is called', () => {
      // Arrange
      const initialCount = component.count;

      // Act
      component.increment();

      // Assert
      expect(component.count).toBe(initialCount + 1);
    });

    it('should increment multiple times correctly', () => {
      // Arrange
      const incrementTimes = 5;

      // Act
      for (let i = 0; i < incrementTimes; i++) {
        component.increment();
      }

      // Assert
      expect(component.count).toBe(incrementTimes);
    });
  });

  describe('Async Operations', () => {
    /**
     * Example of async testing with mockResolvedValue pattern
     */
    it('should handle async data fetching', async () => {
      // Arrange
      expect(component.loading).toBe(false);

      // Act
      const fetchPromise = component.fetchData();
      
      // Assert - loading state should be true during fetch
      expect(component.loading).toBe(true);
      
      // Wait for async operation to complete
      await fetchPromise;
      
      // Assert - loading state should be false after fetch
      expect(component.loading).toBe(false);
    });

    /**
     * Example of testing with fake timers
     */
    it('should use fake timers for time-dependent functionality', () => {
      // Arrange
      jest.useFakeTimers();
      const callback = jest.fn();

      // Act
      setTimeout(callback, 1000);
      jest.advanceTimersByTime(1000);

      // Assert
      expect(callback).toHaveBeenCalledTimes(1);

      // Cleanup
      jest.useRealTimers();
    });

    /**
     * Example using waitFor for async state changes
     */
    it('should wait for async state changes', async () => {
      // Arrange & Act
      component.fetchData();

      // Assert - wait for loading to become false
      await waitFor(() => {
        expect(component.loading).toBe(false);
      });
    });
  });

  describe('Mock Functions and Spies', () => {
    /**
     * Example of using mock functions
     */
    it('should demonstrate mock function usage', () => {
      // Arrange
      const mockFn = jest.fn();
      mockFn.mockReturnValue('mocked value');

      // Act
      const result = mockFn('test argument');

      // Assert
      expect(mockFn).toHaveBeenCalledWith('test argument');
      expect(mockFn).toHaveBeenCalledTimes(1);
      expect(result).toBe('mocked value');
    });

    /**
     * Example of using spies
     */
    it('should spy on component methods', () => {
      // Arrange
      const incrementSpy = jest.spyOn(component, 'increment');

      // Act
      component.increment();
      component.increment();

      // Assert
      expect(incrementSpy).toHaveBeenCalledTimes(2);
      expect(component.count).toBe(2);

      // Cleanup
      incrementSpy.mockRestore();
    });

    /**
     * Example of mockResolvedValue for async testing
     */
    it('should demonstrate mockResolvedValue', async () => {
      // Arrange
      const mockAsyncFn = jest.fn().mockResolvedValue({ data: 'test' });

      // Act
      const result = await mockAsyncFn();

      // Assert
      expect(mockAsyncFn).toHaveBeenCalled();
      expect(result).toEqual({ data: 'test' });
    });

    /**
     * Example of mockRejectedValue for error testing
     */
    it('should demonstrate mockRejectedValue for error handling', async () => {
      // Arrange
      const mockAsyncFn = jest.fn().mockRejectedValue(new Error('Test error'));

      // Act & Assert
      await expect(mockAsyncFn()).rejects.toThrow('Test error');
    });
  });

  describe('Data-driven Testing', () => {
    /**
     * Example of testing with different inputs using test.each
     */
    it.each([
      [1, 1],
      [5, 5],
      [10, 10],
    ])('should increment count to %i after %i calls', (times, expected) => {
      // Act
      for (let i = 0; i < times; i++) {
        component.increment();
      }

      // Assert
      expect(component.count).toBe(expected);
    });
  });

  describe('Error Handling', () => {
    it('should handle errors gracefully', () => {
      // Arrange
      const errorFn = () => {
        throw new Error('Test error');
      };

      // Act & Assert
      expect(errorFn).toThrow('Test error');
      expect(errorFn).toThrow(Error);
    });
  });
});

/**
 * Example of snapshot testing (use sparingly for stable UI components)
 */
describe('SampleComponent Snapshot', () => {
  it('should match snapshot', () => {
    const component = new SampleComponent();
    expect(component).toMatchSnapshot();
  });
});

/**
 * Example of testing with Angular Testing Library
 */
describe('SampleComponent with Testing Library', () => {
  it('should render component using Testing Library', async () => {
    // This is a simplified example - actual implementation would need a real Angular component
    // See Angular Testing Library documentation for complete examples
    
    const mockComponent = {
      title: 'Sample Component',
      count: 0
    };

    expect(mockComponent.title).toBe('Sample Component');
    expect(mockComponent.count).toBe(0);
  });
});

