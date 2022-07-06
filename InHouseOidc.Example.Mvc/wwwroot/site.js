function displayAccessTokenEncoded() {
    document.getElementById('buttonAccessTokenEncoded').classList.add('active');
    document.getElementById('buttonAccessTokenDecoded').classList.remove('active');
    document.getElementById('accessTokenEncoded').classList.remove('d-none');
    document.getElementById('accessTokenDecoded').classList.add('d-none');
}
function displayAccessTokenDecoded() {
    document.getElementById('buttonAccessTokenEncoded').classList.remove('active');
    document.getElementById('buttonAccessTokenDecoded').classList.add('active');
    document.getElementById('accessTokenEncoded').classList.add('d-none');
    document.getElementById('accessTokenDecoded').classList.remove('d-none');
}
function displayIdTokenEncoded() {
    document.getElementById('buttonIdTokenEncoded').classList.add('active');
    document.getElementById('buttonIdTokenDecoded').classList.remove('active');
    document.getElementById('idTokenEncoded').classList.remove('d-none');
    document.getElementById('idTokenDecoded').classList.add('d-none');
}
function displayIdTokenDecoded() {
    document.getElementById('buttonIdTokenEncoded').classList.remove('active');
    document.getElementById('buttonIdTokenDecoded').classList.add('active');
    document.getElementById('idTokenEncoded').classList.add('d-none');
    document.getElementById('idTokenDecoded').classList.remove('d-none');
}
