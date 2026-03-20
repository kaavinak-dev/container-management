FROM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /app
COPY . .
RUN chmod +x os-process-manager-service
EXPOSE 5000 5001
ENTRYPOINT ["./os-process-manager-service"]
