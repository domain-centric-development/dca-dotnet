# Resolves the NuGet.org API key into NUGET_API_KEY. Sources, in order: the environment, the macOS
# keychain item "dca-nuget", an interactive prompt. Meant to be sourced, not executed.

KEYCHAIN_ITEM="${DCA_NUGET_KEYCHAIN_ITEM:-dca-nuget}"

if [ -n "${NUGET_API_KEY:-}" ]; then
  echo "using the NuGet API key from the environment"
elif _dca_key="$(security find-generic-password -s "$KEYCHAIN_ITEM" -w 2>/dev/null)"; then
  export NUGET_API_KEY="$_dca_key"
  unset _dca_key
  echo "using the NuGet API key from the keychain item $KEYCHAIN_ITEM"
else
  echo "no NuGet API key in the environment or keychain item $KEYCHAIN_ITEM"
  read -r -s -p "NuGet.org API key: " _dca_key
  echo
  export NUGET_API_KEY="$_dca_key"
  unset _dca_key
fi

[ -n "$NUGET_API_KEY" ] || { echo "error: no NuGet API key" >&2; exit 1; }
