def process_data(items):
    # Test list comprehension
    squared = [x for x in items if x > 0]
    
    # Test lambda
    double = lambda x: x * 2
    
    # Test try/except/finally
    try:
        result = process_file("test.json")
    except FileNotFoundError as e:
        print("File not found")
    finally:
        print("Cleanup")
    
    return squared

class DataProcessor:
    def process(self):
        values = [1, 2, 3, 4, 5]
        filtered = [v for v in values if v % 2 == 0]
        return filtered
