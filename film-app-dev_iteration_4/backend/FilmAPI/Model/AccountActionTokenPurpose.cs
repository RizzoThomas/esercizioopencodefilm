namespace FilmAPI.Model;

/// <summary>
/// Scopo del token temporaneo collegato all'account nella piattaforma CineBase.
/// Serve ai servizi di sicurezza per sapere quale azione sensibile autorizzare e mappa un enum persistito nel database.
/// </summary>
public enum AccountActionTokenPurpose
{
    /// <summary>Token per reimpostare la password dell'account.</summary>
    PasswordReset = 0,
    /// <summary>Token per impostare la password iniziale o sostitutiva.</summary>
    SetPassword = 1,
    /// <summary>Token per completare un invito amministrativo.</summary>
    AdminInvite = 2
}
