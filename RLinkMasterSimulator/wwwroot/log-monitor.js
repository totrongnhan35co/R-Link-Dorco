let _status = [], _camera = [], _cameraError = [];
let _autoRefresh = true;

async function fetchLogs() {
    try {
        const res = await fetch('/api/rlink/log/recent');
        if (!res.ok) { console.error('fetchLogs failed:', res.status); return; }
        const d = await res.json();
        _status      = d.status       || [];
        _camera      = d.camera       || [];
        _cameraError = d.camera_error || [];
        renderAll();
        document.getElementById('last-update').textContent =
            'Cap nhat: ' + new Date().toLocaleTimeString('vi-VN');
    } catch (e) { console.error('fetchLogs error', e); }
}

async function fetchSyncStatus() {
    try {
        const res = await fetch('/api/rlink/log/sync-status');
        if (!res.ok) return;
        const d = await res.json();
        const reports = d.reports || [];
        const server  = d.server  || {};

        document.getElementById('srv-in').textContent  = server.received_in     || 0;
        document.getElementById('srv-cam').textContent = server.received_camera || 0;
        document.getElementById('srv-err').textContent = server.received_error  || 0;

        const totalPending = reports.reduce(function(s, r) {
            return s + (r.pending_in || 0) + (r.pending_camera || 0) + (r.pending_error || 0);
        }, 0);
        document.getElementById('badge-sync').textContent = totalPending > 0 ? ('! ' + totalPending) : reports.length;

        // ── Bảng tổng theo Line ────────────────────────────────────
        var lineTbody = document.getElementById('body-sync');
        if (!reports.length) {
            lineTbody.innerHTML = '<tr class="empty-row"><td colspan="8">Chua co bao cao tu client R-Link</td></tr>';
        } else {
            lineTbody.innerHTML = reports.map(function(r) {
                var allOk  = !r.pending_in && !r.pending_camera && !r.pending_error;
                var action = allOk
                    ? '<span style="color:#86efac;font-size:12px;">OK Dong bo</span>'
                    : '<button class="btn btn-primary" style="padding:4px 10px;font-size:12px;" onclick="triggerResync(\'' + r.line_id + '\')">Dong bo ngay</button>';
                return '<tr>'
                    + '<td><b>' + (r.line_id || '-') + '</b></td>'
                    + '<td>' + (r.rlink_name || '-') + '</td>'
                    + '<td class="' + (r.pending_in     > 0 ? 'num-fail' : 'num-ok') + '">' + (r.pending_in     || 0) + '</td>'
                    + '<td class="' + (r.pending_camera > 0 ? 'num-fail' : 'num-ok') + '">' + (r.pending_camera || 0) + '</td>'
                    + '<td class="' + (r.pending_error  > 0 ? 'num-fail' : 'num-ok') + '">' + (r.pending_error  || 0) + '</td>'
                    + '<td>' + fmtTime(r.reported_at) + '</td>'
                    + '<td>' + action + '</td>'
                    + '</tr>';
            }).join('');
        }

        // ── Bảng chi tiết theo Job ─────────────────────────────────
        var jobTbody = document.getElementById('body-sync-job');
        var allJobs = [];
        reports.forEach(function(r) {
            var jobs = r.job_details || [];
            jobs.forEach(function(j) {
                allJobs.push({ line_id: r.line_id, rlink_name: r.rlink_name, job: j });
            });
        });

        if (!allJobs.length) {
            jobTbody.innerHTML = '<tr class="empty-row"><td colspan="7">Tat ca da dong bo</td></tr>';
        } else {
            jobTbody.innerHTML = allJobs.map(function(item) {
                var j = item.job;
                var total = (j.pending_in || 0) + (j.pending_camera || 0) + (j.pending_error || 0);
                return '<tr>'
                    + '<td>' + (item.line_id || '-') + '</td>'
                    + '<td style="font-weight:600;color:#e0e0e0">' + (j.job_name || '-') + '</td>'
                    + '<td style="font-size:11px;color:#9ca3af">' + (j.batch || '-') + '</td>'
                    + '<td class="' + (j.pending_in     > 0 ? 'num-fail' : 'num-ok') + '">' + (j.pending_in     || 0) + '</td>'
                    + '<td class="' + (j.pending_camera > 0 ? 'num-fail' : 'num-ok') + '">' + (j.pending_camera || 0) + '</td>'
                    + '<td class="' + (j.pending_error  > 0 ? 'num-fail' : 'num-ok') + '">' + (j.pending_error  || 0) + '</td>'
                    + '<td class="' + (total            > 0 ? 'num-fail' : 'num-ok') + '"><b>' + total + '</b></td>'
                    + '</tr>';
            }).join('');
        }

    } catch (e) { console.error('fetchSyncStatus error', e); }
}

async function triggerResync(lineId) {
    try {
        await fetch('/api/rlink/log/resync-request?line_id=' + encodeURIComponent(lineId), { method: 'POST' });
        alert('Da gui yeu cau dong bo cho line="' + lineId + '".');
    } catch (e) { alert('Loi: ' + e.message); }
}

function fmtTime(ts) {
    if (!ts) return '-';
    try { return new Date(ts).toLocaleString('vi-VN'); } catch(e) { return ts; }
}

function filterRows(rows, filterVal) {
    if (!filterVal) return rows;
    var q = filterVal.toLowerCase();
    return rows.filter(function(r) { return JSON.stringify(r).toLowerCase().indexOf(q) >= 0; });
}

function statusBadge(s) {
    var map = { start:'s-start', running:'s-running', stop:'s-stop', completed:'s-completed', error:'s-error' };
    var cls = (s && map[s.toLowerCase()]) ? map[s.toLowerCase()] : 's-stop';
    return '<span class="badge-status ' + cls + '">' + (s || '-') + '</span>';
}

function renderStatus() {
    var rows = filterRows(_status, document.getElementById('filter-status').value);
    document.getElementById('badge-status').textContent = _status.length;
    var tbody = document.getElementById('body-status');
    if (!rows.length) { tbody.innerHTML = '<tr class="empty-row"><td colspan="8">Khong co du lieu</td></tr>'; return; }
    tbody.innerHTML = rows.map(function(r) {
        return '<tr>'
            + '<td>' + fmtTime(r.timestamp) + '</td>'
            + '<td>' + (r.line_id || '-') + '</td>'
            + '<td>' + (r.rlink_name || '-') + '</td>'
            + '<td>' + (r.job_name || '-') + '</td>'
            + '<td>' + (r.batch || '-') + '</td>'
            + '<td>' + statusBadge(r.status) + '</td>'
            + '<td class="num-total">' + (r.produced != null ? r.produced : (r.qty || 0)) + '</td>'
            + '<td>' + (r.operator_user || '-') + '</td>'
            + '</tr>';
    }).join('');
}

function renderCamera() {
    var rows = filterRows(_camera, document.getElementById('filter-camera').value);
    document.getElementById('badge-camera').textContent = _camera.length;
    var tbody = document.getElementById('body-camera');
    if (!rows.length) { tbody.innerHTML = '<tr class="empty-row"><td colspan="9">Khong co du lieu</td></tr>'; return; }
    tbody.innerHTML = rows.map(function(r) {
        var ok    = r.camera_ok   != null ? r.camera_ok   : 0;
        var fail  = r.camera_fail != null ? r.camera_fail : 0;
        var total = r.total_check != null ? r.total_check : (ok + fail);
        return '<tr>'
            + '<td>' + fmtTime(r.timestamp) + '</td>'
            + '<td>' + (r.line_id || '-') + '</td>'
            + '<td>' + (r.rlink_name || '-') + '</td>'
            + '<td>' + (r.job_name || '-') + '</td>'
            + '<td>' + (r.batch || '-') + '</td>'
            + '<td class="num-ok">' + ok + '</td>'
            + '<td class="num-fail">' + fail + '</td>'
            + '<td class="num-total">' + total + '</td>'
            + '<td>' + (r.operator_user || '-') + '</td>'
            + '</tr>';
    }).join('');
}

function renderError() {
    var rows = filterRows(_cameraError, document.getElementById('filter-error').value);
    document.getElementById('badge-error').textContent = _cameraError.length;
    var tbody = document.getElementById('body-error');
    if (!rows.length) { tbody.innerHTML = '<tr class="empty-row"><td colspan="9">Khong co du lieu</td></tr>'; return; }
    tbody.innerHTML = rows.map(function(r) {
        return '<tr>'
            + '<td>' + fmtTime(r.timestamp) + '</td>'
            + '<td>' + (r.line_id || '-') + '</td>'
            + '<td>' + (r.job_name || '-') + '</td>'
            + '<td>' + (r.batch || '-') + '</td>'
            + '<td style="font-family:monospace;font-size:11px">' + (r.qr_code || '-') + '</td>'
            + '<td><span class="badge-status s-error">' + (r.error_type || '-') + '</span></td>'
            + '<td style="max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap" title="' + (r.error_message || '') + '">' + (r.error_message || '-') + '</td>'
            + '<td>' + (r.frame_index != null ? r.frame_index : 0) + '</td>'
            + '<td>' + (r.operator_user || '-') + '</td>'
            + '</tr>';
    }).join('');
}

function renderAll() { renderStatus(); renderCamera(); renderError(); }

function switchTab(name) {
    document.querySelectorAll('.tab').forEach(function(t) { t.classList.remove('active'); });
    document.querySelectorAll('.panel').forEach(function(p) { p.classList.remove('active'); });
    var idx = { status: 0, camera: 1, 'camera-error': 2, sync: 3 }[name];
    document.querySelectorAll('.tab')[idx].classList.add('active');
    document.getElementById('panel-' + name).classList.add('active');
    if (name === 'sync') fetchSyncStatus();
}

function clearFilter(id) { document.getElementById(id).value = ''; renderAll(); }

document.addEventListener('DOMContentLoaded', function() {
    document.getElementById('refresh-toggle').addEventListener('change', function() {
        _autoRefresh = this.checked;
    });
    setInterval(function() { if (_autoRefresh) { fetchLogs(); fetchSyncStatus(); } }, 3000);
    fetchLogs();
    fetchSyncStatus();
});