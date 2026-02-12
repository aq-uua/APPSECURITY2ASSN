namespace WebApplication3.Model;

public enum AuditEventType
{
    LoginSuccess,
    LoginFailure,
    Logout,
    SessionExpired,
    SessionTerminated,
    PasswordChanged,
    PasswordChangeFailed,
    PasswordExpired,
    RecaptchaFailed,
    PasswordResetRequested,
    PasswordResetSucceeded,
    PasswordResetFailed,
    TwoFactorEnabled,
    TwoFactorDisabled,
    TwoFactorChallengeSent,
    TwoFactorChallengeSucceeded,
    TwoFactorChallengeFailed,
    RecoveryCodesGenerated,
    RecoveryCodeUsed,
    ProfileUpdated,
    DeliveryUpdated,
    PaymentUpdated,
    PhotoUpdated,
    EmailVerified
}
