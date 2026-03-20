#!/bin/sh
# Start the user process in the background, capture its PID
$@ &
USER_PID=$!

# Write PID to a known location so the sidecar can find it on startup
echo $USER_PID > /tmp/user-process.pid

# Start the sidecar in the foreground — the container lives as long as the sidecar
/sidecar/os-process-manager-service

# If the sidecar exits, bring down the user process too
kill $USER_PID
