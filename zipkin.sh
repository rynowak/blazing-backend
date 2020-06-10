#!/usr/bin/env bash
set -x
kubectl port-forward svc/zipkin -n zipkin 9411:9411