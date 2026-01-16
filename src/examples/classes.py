import os
import json

def process_file(filename):
    path = os.path.join("data", filename)
    data = "file content"
    return data

def main():
    filename = "config.json"
    content = process_file(filename)
    print("Processing complete")

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
