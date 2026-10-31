#!/bin/sh
# Same as change-demo-data.ps1 for non-Windows hosts.
B=${BASE:-http://127.0.0.1:5080}; K=${API_KEY:-demo-key-change-me}
curl -fsS -X POST -H "X-Api-Key: $K" "$B/api/demo/update?datasetId=sales-summary&key=North&value=${1:-150000}" && echo " updated"
