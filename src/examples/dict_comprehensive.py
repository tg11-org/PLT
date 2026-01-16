# Comprehensive dictionary literals example
# Demonstrates all supported dictionary features in PLT

# Basic dictionaries
config = {"host": "localhost", "port": "8080"}
empty = {}
flags = {1: "true", 0: "false"}

# Complex nested structure
app = {
    "name": "MyApp",
    "version": "1.0",
    "settings": {
        "debug": "true",
        "timeout": "30"
    },
    "endpoints": {
        "api": "/api/v1",
        "health": "/health"
    }
}

# Mixed types
data = {
    "count": 42,
    "ratio": 3.14,
    "status": "active",
    "metadata": {"id": 1, "type": "object"}
}

# Print examples
print(config)
print(app)
print(data)
