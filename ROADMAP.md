# Roadmap

## Blocked

### ECC Curve25519/448 Server (`ECC_curve25519`, `ECC_curve448`)

**Status:** Blocked by upstream SDK limitation

**What:** A dedicated server instance (`opcua-ecc-curve`, port 4850) exposing the `ECC_curve25519` and `ECC_curve448` security policies with Ed25519/Ed448 (EdDSA) signatures and X25519/X448 key agreement.

**Why it's blocked:**

The [OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard) SDK (v1.5.378) does not support auto-generating application certificates for the `EccCurve25519ApplicationCertificateType` and `EccCurve448ApplicationCertificateType` certificate types. When configured, the SDK throws:

```
Fatal error: The Ecc certificate type is not supported.
  at Opc.Ua.Configuration.ApplicationInstance.CreateApplicationInstanceCertificateAsync(...)
```

The SDK's `EccUtils.GetCurveFromCertificateTypeId()` method does not map these certificate type identifiers to the corresponding elliptic curves. While the security policy URIs (`SecurityPolicies.ECC_curve25519`, `SecurityPolicies.ECC_curve448`) and the certificate type ObjectTypeIds (`ObjectTypeIds.EccCurve25519ApplicationCertificateType`, `ObjectTypeIds.EccCurve448ApplicationCertificateType`) are defined in the SDK constants, the server-side certificate generation and secure channel implementation for these curves is not yet complete.

Additionally, .NET's `X509CertificateLoader` cannot load Ed25519 keys from PKCS#12 (PFX) files — OID `1.3.101.112` (Ed25519) is reported as unknown — which prevents manual certificate provisioning as a workaround.

**What needs to happen:**

1. UA-.NETStandard SDK adds Curve25519/448 support in `EccUtils.GetCurveFromCertificateTypeId()` and `CreateApplicationInstanceCertificateAsync()`
2. .NET runtime adds Ed25519/Ed448 support in `X509CertificateLoader` for PKCS#12
