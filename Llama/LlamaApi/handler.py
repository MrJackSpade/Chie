import subprocess
import requests
import runpod
import requests
import time
import json

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
    # Only skip the check if the service is already known to be ready
    if handler.service_ready is not True:
        handler.service_ready = wait_for_service_to_be_ready("http://localhost:5000/")
    
    if handler.service_ready:
        # The service is ready, perform the job processing
        print("Processing job...")
        # Your job processing logic here
    else:
        # Service is not ready, handler.service_ready is set to None for a retry on next call
        print("Cannot process job, service is not ready.")
        handler.service_ready = None

    # Parse the job input, which is a JSON object containing the request details.
    job_input = job["input"]

    # Construct the internal request URL. Assume your .NET app is listening on localhost:80.
    internal_url = f"http://localhost:5000{job_input['url']}"

    # Initialize an empty response dictionary
    response_data = {}


    try:
        if job_input['method'].lower() == 'post':
            # If job_input['body'] is a base64 encoded string
            response = requests.post(internal_url, data=job_input['body'])
        else:
            # Add handling for other HTTP methods as needed.
            response = requests.get(internal_url)

        # Set the status code in the response data
        response_data['status'] = response.status_code

        # Since you're keeping the response as base64, no need to decode it.
        # Just return it as a string.
        response_data['body'] = response.text

    except Exception as e:
        # Handle any exceptions that might occur
        response_data['status'] = 'Error'
        response_data['body'] = str(e)

    except requests.exceptions.RequestException as e:
        # Handle any exceptions that occur during the HTTP request.
        response_data = {
            'status': 500,
            'body': str(e)
        }

    # Return the response_data as a JSON object.
    return json.dumps(response_data)

# Initialize the function attribute to None
handler.service_ready = None

# Start the Runpod serverless handler.
runpod.serverless.start({"handler": handler})
