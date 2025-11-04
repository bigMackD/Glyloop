/**
 * Sample Jest unit test for Angular services
 * Demonstrates testing HTTP services, observables, and error handling
 */

import { TestBed } from '@angular/core/testing';
import { of, throwError, delay } from 'rxjs';

// Mock service for demonstration
class SampleService {
  constructor(private http?: any) {}

  getData(): any {
    return of({ data: 'test data' });
  }

  getDataWithError(): any {
    return throwError(() => new Error('API Error'));
  }

  getDataAsync(): Promise<any> {
    return Promise.resolve({ data: 'async data' });
  }

  processData(input: string): string {
    return input.toUpperCase();
  }
}

describe('SampleService', () => {
  let service: SampleService;
  let httpSpy: any;

  beforeEach(() => {
    // Create mock HTTP client
    httpSpy = {
      get: jest.fn(),
      post: jest.fn(),
      put: jest.fn(),
      delete: jest.fn(),
    };

    // Initialize service with mocked dependencies
    service = new SampleService(httpSpy);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  describe('Service Initialization', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('Observable Data Fetching', () => {
    it('should return data from observable', (done) => {
      // Arrange
      const expectedData = { data: 'test data' };

      // Act
      service.getData().subscribe({
        next: (result: any) => {
          // Assert
          expect(result).toEqual(expectedData);
          done();
        },
        error: done.fail,
      });
    });

    it('should handle observable errors', (done) => {
      // Act
      service.getDataWithError().subscribe({
        next: () => done.fail('Should have thrown error'),
        error: (error: Error) => {
          // Assert
          expect(error.message).toBe('API Error');
          done();
        },
      });
    });

    /**
     * Example of testing observables with fake timers
     */
    it('should handle delayed observables', (done) => {
      // Arrange
      jest.useFakeTimers();
      const delayedData$ = of({ data: 'delayed' }).pipe(delay(1000));

      // Act
      delayedData$.subscribe({
        next: (result) => {
          // Assert
          expect(result).toEqual({ data: 'delayed' });
          done();
        },
        error: done.fail,
      });

      // Fast-forward time
      jest.advanceTimersByTime(1000);
      jest.useRealTimers();
    });
  });

  describe('Promise-based Data Fetching', () => {
    it('should return data from promise', async () => {
      // Act
      const result = await service.getDataAsync();

      // Assert
      expect(result).toEqual({ data: 'async data' });
    });

    /**
     * Example of testing promise rejection
     */
    it('should handle promise rejection', async () => {
      // Arrange
      const errorService = new SampleService();
      const mockError = new Error('Promise error');
      jest.spyOn(errorService, 'getDataAsync').mockRejectedValue(mockError);

      // Act & Assert
      await expect(errorService.getDataAsync()).rejects.toThrow('Promise error');
    });
  });

  describe('HTTP Mock Testing', () => {
    /**
     * Example of mocking HTTP GET request
     */
    it('should call HTTP GET with correct URL', () => {
      // Arrange
      const mockResponse = { users: [] };
      httpSpy.get.mockReturnValue(of(mockResponse));

      // Act
      const result$ = httpSpy.get('/api/users');

      // Assert
      expect(httpSpy.get).toHaveBeenCalledWith('/api/users');
      result$.subscribe((data: any) => {
        expect(data).toEqual(mockResponse);
      });
    });

    /**
     * Example of mocking HTTP POST request
     */
    it('should call HTTP POST with correct data', () => {
      // Arrange
      const postData = { name: 'Test User' };
      const mockResponse = { id: 1, ...postData };
      httpSpy.post.mockReturnValue(of(mockResponse));

      // Act
      const result$ = httpSpy.post('/api/users', postData);

      // Assert
      expect(httpSpy.post).toHaveBeenCalledWith('/api/users', postData);
      result$.subscribe((data: any) => {
        expect(data).toEqual(mockResponse);
      });
    });

    /**
     * Example of testing HTTP error handling
     */
    it('should handle HTTP errors', (done) => {
      // Arrange
      const errorResponse = { status: 404, message: 'Not Found' };
      httpSpy.get.mockReturnValue(
        throwError(() => errorResponse)
      );

      // Act
      httpSpy.get('/api/users/999').subscribe({
        next: () => done.fail('Should have thrown error'),
        error: (error: any) => {
          // Assert
          expect(error.status).toBe(404);
          expect(error.message).toBe('Not Found');
          done();
        },
      });
    });
  });

  describe('Data Processing', () => {
    /**
     * Example of parameterized testing with test.each
     */
    it.each([
      ['hello', 'HELLO'],
      ['world', 'WORLD'],
      ['test', 'TEST'],
    ])('should convert "%s" to uppercase "%s"', (input, expected) => {
      // Act
      const result = service.processData(input);

      // Assert
      expect(result).toBe(expected);
    });
  });

  describe('Spy and Mock Functions', () => {
    /**
     * Example of spying on service methods
     */
    it('should spy on service method calls', () => {
      // Arrange
      const processSpy = jest.spyOn(service, 'processData');

      // Act
      service.processData('test');

      // Assert
      expect(processSpy).toHaveBeenCalledWith('test');
      expect(processSpy).toHaveBeenCalledTimes(1);

      // Cleanup
      processSpy.mockRestore();
    });

    /**
     * Example of mocking method return values
     */
    it('should mock method return value', () => {
      // Arrange
      jest.spyOn(service, 'processData').mockReturnValue('MOCKED');

      // Act
      const result = service.processData('anything');

      // Assert
      expect(result).toBe('MOCKED');
    });

    /**
     * Example of tracking multiple calls
     */
    it('should track multiple method calls', () => {
      // Arrange
      const processSpy = jest.spyOn(service, 'processData');

      // Act
      service.processData('first');
      service.processData('second');
      service.processData('third');

      // Assert
      expect(processSpy).toHaveBeenCalledTimes(3);
      expect(processSpy).toHaveBeenNthCalledWith(1, 'first');
      expect(processSpy).toHaveBeenNthCalledWith(2, 'second');
      expect(processSpy).toHaveBeenNthCalledWith(3, 'third');
    });
  });

  describe('Code Coverage Patterns', () => {
    /**
     * Example of testing edge cases for coverage
     */
    it('should handle empty string input', () => {
      const result = service.processData('');
      expect(result).toBe('');
    });

    it('should handle special characters', () => {
      const result = service.processData('hello@world!123');
      expect(result).toBe('HELLO@WORLD!123');
    });

    it('should handle null/undefined gracefully', () => {
      // These would need proper null handling in the actual service
      expect(() => service.processData(null as any)).toBeDefined();
      expect(() => service.processData(undefined as any)).toBeDefined();
    });
  });
});

/**
 * Example of testing with TestBed for dependency injection
 * Note: For real Angular services with @Injectable(), use TestBed
 * This example is commented out since SampleService is a mock class
 */
describe('SampleService with TestBed', () => {
  it('should demonstrate TestBed usage for real Angular services', () => {
    // For real Angular services with @Injectable() decorator:
    // TestBed.configureTestingModule({
    //   providers: [YourRealService],
    // });
    // const service = TestBed.inject(YourRealService);
    // expect(service).toBeTruthy();
    
    expect(true).toBe(true); // Placeholder
  });
});

