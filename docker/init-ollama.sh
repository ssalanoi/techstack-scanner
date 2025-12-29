#!/bin/bash
# Initialize Ollama with llama3.2 model

set -e

echo "Waiting for Ollama service to be ready..."
until curl -s http://localhost:11434/api/tags > /dev/null; do
    echo "Ollama not ready yet, waiting..."
    sleep 5
done

echo "Ollama is ready! Pulling model: llama3.2"
ollama pull llama3.2

echo "Model llama3.2 successfully pulled!"
echo "Available models:"
ollama list
