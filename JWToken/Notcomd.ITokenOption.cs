using System.Security.Claims;

namespace Notcomd.Token.JWT
{
    public abstract class ITokenOption
    {
        public abstract IEnumerable<Claim> GetClaimsPten<Type>(Type Item1, Type Item2);
        public abstract IEnumerable<Claim> GetClaimsPten<Types>(Types Item1);
        public abstract IEnumerable<Claim> GetClaims(Type claim);

    }
}
