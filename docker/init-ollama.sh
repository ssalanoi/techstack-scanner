#!/bin/bash
# Initialize Ollama with the specified model

set -e

echo "Waiting for Ollama service to be ready..."
until curl -s http://ollama:11434/api/tags > /dev/null; do
    echo "Ollama not ready yet, waiting..."
    sleep 5
done

echo "Ollama is ready! Pulling model: ${OLLAMA_MODEL:-llama3.2}"
ollama pull ${OLLAMA_MODEL:-llama3.2}

echo "Model successfully pulled!"
echo "Available models:"
ollama list
