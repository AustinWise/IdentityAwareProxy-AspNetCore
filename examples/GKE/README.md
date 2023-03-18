

This runs the Sandwich Tracker on GKE. It is based on these guides:

* [Enabling IAP for GKE](https://cloud.google.com/iap/docs/enabling-kubernetes-howto)
* [Using Google-managed SSL certificates](https://cloud.google.com/kubernetes-engine/docs/how-to/managed-certs)

Besides the files here, some manual steps are required:

```bash
gcloud compute addresses create gke-ingress --global
kubectl create secret generic oauth-secret --from-literal=client_id=client_id_key \
    --from-literal=client_secret=client_secret_key
```

Where `client_id_key` and `client_secret_key` are from the OAuth credentials
page in the Console.

Also, an A DNS record needs to created to point sandwich-gke-awise.us at the load balancer IP.
To find the IP:

```bash
gcloud compute addresses describe gke-ingress --global
```
