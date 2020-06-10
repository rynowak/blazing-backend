#!/usr/bin/env bash
set -x
kubectl port-forward svc/kibana-kibana -n elastic 5601:5601