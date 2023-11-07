import subprocess
import requests
import runpod
import requests
import time

# Start the .NET application as a subprocess
subprocess.Popen(["dotnet", "LlamaApi.dll"])

def wait_for_service_to_be_ready(internal_url, retries=30, delay=1):
    for i in range(retries):
        try:
            response = requests.get(internal_url)
            if response.status_code == 200:
                print("Service is ready!")
                return True
        except requests.ConnectionError as e:
            print(f"Service not ready yet, retrying in {delay} seconds...")
            time.sleep(delay)
    print("Service failed to start.")
    return False

def handler(job):
    # Parse the job input, which is a JSON object containing the request details.
    job_input = job["input"]

    service_ready = wait_for_service_to_be_ready("http://localhost:80/")

    # Construct the internal request URL. Assume your .NET app is listening on localhost:5000.
    internal_url = f"http://localhost:80{job_input['url']}"

    # Make the internal request and capture the response.
    if job_input['method'].lower() == 'post':
        response = requests.post(internal_url, json=job_input['body'])
    else:
        # Add handling for other HTTP methods as needed.
        response = requests.get(internal_url)

    # Return the response text or JSON as required.
    # This can be tailored based on the expected response format.
    return response.text or response.json()

# Start the Runpod serverless handler.
runpod.serverless.start({"handler": handler})
