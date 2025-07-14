FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["TimeClockUI/TimeClockUI.csproj", "TimeClockUI/"]
COPY ["TimeClock.client/TimeClock.client.csproj", "TimeClock.client/"]
RUN dotnet restore "TimeClockUI/TimeClockUI.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/TimeClockUI"

# Build and publish the project
RUN dotnet build "TimeClockUI.csproj" -c Release -o /app/build
RUN dotnet publish "TimeClockUI.csproj" -c Release -o /app/publish

# Use nginx to serve the published static files
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .

# Copy appsettings.json
COPY --from=build /src/TimeClockUI/appsettings.json .

# Install necessary tools
RUN apk add --no-cache jq

# Add the entrypoint script
COPY TimeClockUI/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh && \
    # Ensure the file has LF line endings
    sed -i 's/\r$//' /docker-entrypoint.sh

# Configure nginx
COPY TimeClockUI/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80

# Execute the script on container startup
CMD /docker-entrypoint.sh && nginx -g "daemon off;"
