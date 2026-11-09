mergeInto(LibraryManager.library, { BridgeRuntimeReport: function(value) {
  var text=UTF8ToString(value); var el=document.getElementById('bridge-runtime-result');
  if(!el) { el=document.createElement('pre'); el.id='bridge-runtime-result'; document.body.appendChild(el); }
  el.textContent='BRIDGE_RUNTIME_PLAYER_'+text; document.body.dataset.bridgeRuntimeResult=text;
} });
