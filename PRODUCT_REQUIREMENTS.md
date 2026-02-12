# Fresh Farm Market — Membership Service PRD

Purpose: define requirements, scope, security controls, phased feature delivery, and acceptance criteria for the Fresh Farm Market membership/registration service.

Owner: Product / Engineering
Stakeholders: Product Manager, Engineering, Security, QA, UX

---

## 1. Overview

Fresh Farm Market needs a secure membership service that lets customers register, store profile information, authenticate, and manage accounts. The service must protect personally identifiable information (PII) and payment data, provide robust authentication, and prevent common web attacks.

Key constraints: data privacy, regulatory and PCI considerations (see Security section), and a mobile-friendly registration flow.

## 2. Goals
- Provide a complete, validated registration form that saves members to the database.
- Provide secure authentication and session management.
- Protect credentials and customer data at rest and in transit.
- Prevent automated/bot registrations and common web attacks (XSS, CSRF, SQLi).
- Provide account recovery and password management policies.

## 3. Scope

In scope:
- Registration form and validation (fields described below)
- Login / logout, session handling, and session timeout
- Password policy enforcement (client + server)
- Data encryption for sensitive fields (configurable)
- Anti-bot protection (reCAPTCHA v3)
- Input validation, sanitization, and safe error handling
- Account management (change password, reset password via email/SMS, password history)

Out of scope:
- Payment processing (recommend using a PCI-compliant provider). If the product must store credit card data, a separate PCI-scope implementation and audit will be required.

## 4. User data & Registration Form

Fields required on registration (matches product brief):
- Full Name
- Credit Card No (must be encrypted or tokenized; see Security)
- Gender (enum)
- Mobile No
- Delivery Address (text)
- Email address (must be unique)
- Password
- Confirm Password
- Photo (JPG only)
- About Me (allow all special characters; stored safely)

Validation rules (high level):
- Email must be unique and RFC-valid; verify by sending confirmation email.
- Password minimum 12 characters, mix of lower/upper-case, numbers, and special characters. Enforce both client and server checks.
- Photo: accept only JPG, max file size configurable (e.g., 5 MB), sanitize filename, store in object storage.
- Mobile No: validate format by region; optionally verify via OTP on registration.

## 5. Feature List (detailed requirements)

- Registration (4% weight in brief):
  - Save member info to DB; prevent duplicate emails; return meaningful errors for duplicates.
  - Account must be inactive until email verification completes.

- Password strength & credential protection (10% + 6% weights):
  - Client-side and server-side password complexity checks.
  - Hash passwords using a modern adaptive algorithm (Argon2 or bcrypt with high cost) and per-user salt.
  - Encryption at rest for sensitive customer data (delivery address, credit card) using application-level encryption or database field-level encryption.
  - Decryption only for authorized display paths; never return raw credit card number to client.

- Session management (10%):
  - Issue secure session on successful login: use HTTP-only, Secure cookies with SameSite flags or use short-lived JWTs with refresh tokens kept in secure store.
  - Session timeout and inactivity logout.
  - Detect and optionally prevent multiple concurrent sessions or provide user visibility to active sessions.

- Login/Logout and credential verification (10%):
  - Login must support rate limiting and account lockout after configurable failed attempts.
  - Proper logout clears server-side session and client tokens and redirects to login.
  - Audit logging: record critical user activities (registration, login success/failure, password reset).

- Anti-bot (5%):
  - Integrate Google reCAPTCHA v3 for registration and high-volume endpoints.

- Input validation and safe error handling (15% + 5%):
  - Sanitize and validate all inputs server-side; implement parameterized queries/ORM to prevent SQL injection.
  - Add CSRF protection for state-changing endpoints.
  - Output-encode user-supplied data in views to prevent XSS.
  - Custom friendly error pages and graceful handling for 400/403/404/500.

- Advanced account policies & recovery (10% + 5%):
  - Account lockout auto-recovery after configurable duration.
  - Password history to prevent reuse (keep last N hashes; N=2 by brief).
  - Password age policy (min time between changes, max age forcing change).
  - Reset password via secure link delivered to email (or OTP via SMS) with short expiry.
  - 2FA (optional/phase) via authenticator app or SMS backed by rate limiting.

## 6. Phased Delivery (feature-based)

Phase 1 — Registration Form and Storage
- Implement the complete registration form and client-side validation.
- Server endpoints to create account, check duplicate email, store profile with required fields.
- Save photos to object storage and store references.
- Send email verification link after registration.

Acceptance criteria:
- Form saves data to DB; duplicate emails rejected; email verification link sent.

Phase 2 — Authentication & Sessions
- Implement login/logout flows, session issuance, session timeout, and secure cookie handling.
- Implement audit logging for auth events.

Acceptance criteria:
- Users can login/logout; sessions expire after configured inactivity; audit logs recorded.

Phase 3 — Password Policies & Credential Protection
- Server-side password strength enforcement and hashing with Argon2/bcrypt.
- Implement password history and password-change endpoint.
- Implement rate limiting and account lockout logic.

Acceptance criteria:
- Passwords meet policy; cannot reuse restricted passwords; lockout enforced after failed attempts.

Phase 4 — Data Protection & Encryption
- Implement encryption for sensitive fields (credit card, delivery address) at rest.
- Integrate or recommend a PCI-compliant payment/tokenization flow for credit card handling.

Acceptance criteria:
- Sensitive fields stored encrypted and can be decrypted by authorized server-side flows only.

Phase 5 — Validation, Anti-bot & Error Handling
- Implement server-side input validation, CSRF protection, XSS output encoding.
- Integrate reCAPTCHA v3 for registration.
- Custom error pages and messages.

Acceptance criteria:
- Application rejects malformed inputs; reCAPTCHA reduces bot registrations; custom error pages present.

Phase 6 — Account Recovery, 2FA & Monitoring
- Reset password by email/SMS with short-lived tokens.
- Add optional 2FA (TOTP) and multi-device session management UI.
- Implement monitoring, alerts for repeated failed attempts, and audit log retention policy.

Acceptance criteria:
- Password reset works securely; 2FA can be enabled by users; monitoring alerts trigger on suspicious activity.

## 7. Data Model (high level)

User:
- id (uuid)
- email (unique)
- password_hash
- password_history (list of previous password hashes with timestamps)
- full_name
- mobile_number
- gender
- delivery_address_encrypted
- credit_card_token_or_encrypted (recommend tokenization)
- photo_url
- about_me (stored safely, encoded)
- email_verified (bool)
- created_at, updated_at

AuthSession:
- id
- user_id
- session_token or jwt_id
- created_at, last_active_at, expires_at
- device_info, ip_address

AuditLog:
- id, user_id (optional), event_type, details (JSON), created_at, ip

## 8. APIs / Endpoints (examples)

- POST /api/v1/register — create account
- POST /api/v1/login — authenticate
- POST /api/v1/logout — revoke session
- GET /api/v1/session — get active sessions
- POST /api/v1/password/change — change password (auth)
- POST /api/v1/password/reset/request — request reset (email)
- POST /api/v1/password/reset/confirm — confirm reset
- POST /api/v1/2fa/enable — enable TOTP
- POST /api/v1/2fa/verify — verify TOTP

All POST endpoints require CSRF protections where applicable; rate limiting applied to auth and reset endpoints.

## 9. UX & Flow Notes
- Registration: show inline password strength meter and list unmet password rules.
- Prevent form submission until client-side checks pass; still perform server-side validation.
- Show clear messages on duplicate email with CTA to login or reset password.
- After successful registration and email verification, redirect to login or logged-in homepage showing user profile (without raw credit card data).

## 10. Security Requirements (detailed)

- Transport: enforce TLS 1.2+ for all endpoints; HSTS for browsers.
- Password storage: use Argon2id or bcrypt with strong parameters; per-user salts.
- Data encryption: AES-256-GCM for application-level encryption; keys stored in managed KMS.
- Credit card: do not store raw PAN; use third-party PCI compliant gateway or tokenization. If storing is required, obtain PCI compliance and encrypt card data with strict access controls.
- Session cookies: Secure, HttpOnly, SameSite=strict (or Lax depending on cross-site needs). Consider rotating session identifiers after privilege changes.
- CSRF: use anti-CSRF tokens for stateful flows.
- XSS: output encode, CSP headers where feasible.
- SQLi: use parameterized queries/ORM.
- Rate limiting: IP and account-based throttling; account lockout after configurable consecutive failures.
- Audit logging & monitoring: log failed/successful auth attempts, changes to account, and admin actions. Protect logs from tampering and retain per policy.
- Secret management: do not store keys in repo or environment; use vault/KMS.
- File uploads: validate mime type, reject non-JPGs, scan for malware, store in object storage with restricted ACLs.

## 11. Acceptance Criteria (summary)
- All registration fields saved and validated; duplicate email prevented.
- Password policy enforced client & server side; passwords stored hashed.
- Sessions secure and time out; logout clears session.
- Sensitive fields encrypted at rest and never returned to client in raw form.
- Rate limiting and reCAPTCHA implemented.
- Input validation prevents SQLi/XSS/CSRF for tested attack vectors.

## 12. Testing & QA
- Unit tests for validation, hashing & encryption functions.
- Integration tests for registration -> email verification -> login flow.
- Security tests: automated scans for OWASP Top 10 (SAST), dependency checks, and manual pen testing for critical flows.
- Load test to verify rate-limiting & session handling under attack patterns.

## 13. Monitoring & Incident Response
- Alerts for repeated failed logins, anomalous IPs, or sudden spike in registrations.
- Process for revoking compromised sessions and rotating keys.

## 14. Compliance & Privacy
- Where credit card data is involved, follow PCI-DSS. Otherwise, follow regional privacy laws for storage and retention of PII.

## 15. Risks & Mitigations
- Storing credit card PAN: high risk — recommend tokenization with payment provider.
- Insufficient key management: require KMS and rotation policy.
- User privacy concerns: provide data deletion/portability endpoints per compliance.

---

Document created: PRODUCT_REQUIREMENTS.md

## SMTP / Email Sender Implementation (PRD addition)

Rationale: Email delivery is required for account verification, password reset (forgot password), and email-based 2FA. The service must provide a secure, configurable EmailSender that works in development, CI, and production environments.

Requirements:
- Configurable via `appsettings` and environment/user-secrets; secrets must not be committed.
- Support TLS (STARTTLS or SMTPS), authentication, and configurable ports/hosts.
- Provide templated transactional emails for: account verification, password reset (single-use, short expiry), 2FA codes, and administrative notifications.
- Provide robust error handling and retry with exponential backoff for transient SMTP failures; log events but never log sensitive credentials or tokens.
- Support development/test modes: capture emails locally (smtp4dev/MailHog) and support test inbox providers (Mailtrap) for CI verification.
- Provide an interface `IEmailSender` and a production implementation that uses MailKit/SmtpClient; allow swapping in a mock/test implementation for automated tests.

Acceptance Criteria:
- A working `EmailSender` that can send verification and password reset emails in development (using smtp4dev) and CI (using Mailtrap or equivalent) with configurable SMTP settings.
- Email templates stored as Razor views or templating files with placeholder tokens; template rendering tested.
- Email sending is used by registration verification, password reset endpoints, and will be used by the 2FA flows (TOTP delivery via email when enabled).

Dependencies:
- Secrets management (user-secrets / env vars / vault) for SMTP credentials.
- Audit logging to record send attempts and failures.

Notes:
- Do not use email as sole channel for high-risk operations in production without additional protections (e.g., 2FA via authenticator apps or SMS providers with verified delivery).
- Ensure tokens for password reset and verification are single-use and expire quickly (recommend 15 minutes for reset links, configurable).
