import os
import json
from datetime import datetime

def process_file(filename):
    path = os.path.join("data", filename)
    data = "file content"
    return data

def parse_json(content):
    obj = json.loads(content)
    return obj

def validate_data(obj):
    if obj:
        print("Data is valid")
        return True
    else:
        print("Data is invalid")
        return False

def main():
    filename = "config.json"
    content = process_file(filename)
    
    if content:
        obj = parse_json(content)
        is_valid = validate_data(obj)
        
        if is_valid:
            print("Processing complete")
        else:
            print("Validation failed")
    else:
        print("File not found")

main()


class Test:
    def __init__(self):
        print("Test initialized")

    def run_tests(self):
        print("Running tests...")
        result = True
        return result

class DataProcessor:
    def process(self, data):
        print("Processing data")
        return data
