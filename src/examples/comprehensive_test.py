def greet(name):
    print("Hello, " + name)
    return name

def process_numbers(numbers):
    # Try/except/finally
    try:
        total = sum(numbers)
        print("Total: " + total)
    except ValueError as e:
        print("Error processing numbers")
    finally:
        print("Processing complete")
    
    # List comprehension
    doubled = [x * 2 for x in numbers if x > 0]
    
    # Lambda
    square_func = lambda x: x * 2
    
    return doubled

class Calculator:
    def add(self, a, b):
        result = a + b
        return result
    
    def multiply(self, a, b):
        return a * b

class AdvancedCalculator:
    def batch_process(self):
        values = [1, 2, 3, 4, 5]
        
        # List comprehension with filter
        even_values = [v for v in values if v % 2 == 0]
        
        # Lambda as variable
        increment = lambda x: x + 1
        
        return even_values

greet("World")
calc = Calculator()
result = calc.add(5, 3)
print(result)
