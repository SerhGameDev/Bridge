mergeInto(LibraryManager.library, {
  BridgeReport: function (value) {
    var result = UTF8ToString(value);
    document.body.setAttribute('data-bridge-smoke', result);
    var output = document.createElement('pre');
    output.id = 'bridge-smoke-result';
    output.textContent = result;
    document.body.appendChild(output);
  }
});
