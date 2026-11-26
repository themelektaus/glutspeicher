// Shows the values the page "submits"
function show(form, extra) {
  const out = document.querySelector('#out');
  const data = {};
  for (const el of form.querySelectorAll('input')) if (el.name) data[el.name] = el.value;
  Object.assign(data, extra || {});
  out.textContent = JSON.stringify(data);
}
