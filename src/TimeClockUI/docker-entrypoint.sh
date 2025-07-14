#!/bin/sh

# Default path for appsettings
SETTINGS_FILE=/usr/share/nginx/html/appsettings.json

# Check if settings file exists
if [ -f "$SETTINGS_FILE" ]; then
  # Process environment variables and update appsettings.json
  # Handle TimeClientBaseUrl if provided
  if [ ! -z "$TimeClientBaseUrl" ]; then
    echo "Setting TimeClientBaseUrl to $TimeClientBaseUrl"
    # Use jq to modify the JSON file
    cp $SETTINGS_FILE ${SETTINGS_FILE}.tmp
    jq --arg url "$TimeClientBaseUrl" '.ApiBaseUrl = $url' ${SETTINGS_FILE}.tmp > $SETTINGS_FILE
    rm ${SETTINGS_FILE}.tmp
  fi

  # Add other environment variables as needed
  # For example, if you have an AuthUrl in your settings:
  if [ ! -z "$AuthUrl" ]; then
    echo "Setting AuthUrl to $AuthUrl"
    cp $SETTINGS_FILE ${SETTINGS_FILE}.tmp
    jq --arg url "$AuthUrl" '.AuthUrl = $url' ${SETTINGS_FILE}.tmp > $SETTINGS_FILE
    rm ${SETTINGS_FILE}.tmp
  fi

  # Print the final settings for debugging
  echo "Final appsettings.json:"
  cat $SETTINGS_FILE
else
  echo "Warning: $SETTINGS_FILE not found!"
fi
