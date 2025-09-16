# Use a Windows base image since the app is self-contained
FROM mcr.microsoft.com/windows/servercore:ltsc2022

# Set working directory in the container
WORKDIR /app

# Copy published files from host's ./publish folder into the container
COPY . .

# Expose ports used by the app
EXPOSE 5000
EXPOSE 5001

# Set the entry point to your self-contained app
ENTRYPOINT ["os-process-manager-service.exe"]
