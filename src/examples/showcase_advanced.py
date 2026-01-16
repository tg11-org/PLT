# This file demonstrates all newly added advanced Python features

def filter_and_transform(data):
    # List comprehension with filter
    positive_squared = [x * 2 for x in data if x > 0]
    
    # Lambda expression
    increment_by_ten = lambda x: x + 10
    
    return positive_squared

def process_with_error_handling():
    # Try/except/finally blocks
    try:
        config = read_config("settings.json")
        print("Configuration loaded successfully")
    except FileNotFoundError as file_error:
        print("Configuration file not found")
    except ValueError as value_error:
        print("Invalid configuration format")
    finally:
        print("Cleanup completed")

class DataAnalyzer:
    def analyze_batch(self):
        numbers = [10, 20, 30, 40, 50]
        
        # List comprehension filtering even numbers
        even_only = [n for n in numbers if n % 2 == 0]
        
        # Lambda as callback
        process_fn = lambda x: x / 2
        
        return even_only

result = filter_and_transform([1, 2, 3, 4, 5])
print(result)
process_with_error_handling()
analyzer = DataAnalyzer()
analyzed = analyzer.analyze_batch()
