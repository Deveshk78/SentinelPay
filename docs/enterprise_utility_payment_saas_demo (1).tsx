import React, { useState, useEffect } from 'react';
import { 
  Activity, ShieldCheck, Cpu, Database, Zap, Flame, Droplets, Wind, 
  Search, RefreshCw, Play, Pause, CheckCircle2, AlertTriangle, ShieldAlert,
  ArrowDownRight, Server, Terminal, Smartphone, 
  Layers, Bot, Send, Sparkles, CreditCard, ChevronRight,
  Lock, Wifi, WifiOff, Globe, HardDrive, Palette, Settings,
  TrendingUp, Fingerprint, Building2, Eye, FileCode
} from 'lucide-react';
import { 
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer
} from 'recharts';

// 15 Themes Definition with full Tailwind design tokens
const THEMES = [
  { id: 'cyber-dark', name: 'Cyber Dark (Default)', bg: 'bg-slate-950', text: 'text-slate-100', cardBg: 'bg-slate-900/70', border: 'border-slate-800', accent: 'cyan', accentBg: 'bg-cyan-500', accentText: 'text-cyan-400', headerBg: 'bg-slate-900/80', previewBg: '#020617' },
  { id: 'enterprise-light', name: 'Enterprise Light', bg: 'bg-slate-50', text: 'text-slate-900', cardBg: 'bg-white', border: 'border-slate-200', accent: 'blue', accentBg: 'bg-blue-600', accentText: 'text-blue-600', headerBg: 'bg-white/80', previewBg: '#f8fafc' },
  { id: 'midnight-violet', name: 'Midnight Violet', bg: 'bg-gray-950', text: 'text-gray-100', cardBg: 'bg-purple-950/40', border: 'border-purple-900/50', accent: 'purple', accentBg: 'bg-purple-600', accentText: 'text-purple-400', headerBg: 'bg-gray-900/80', previewBg: '#030712' },
  { id: 'emerald-grid', name: 'Emerald Power', bg: 'bg-zinc-950', text: 'text-zinc-100', cardBg: 'bg-emerald-950/30', border: 'border-emerald-900/40', accent: 'emerald', accentBg: 'bg-emerald-500', accentText: 'text-emerald-400', headerBg: 'bg-zinc-900/80', previewBg: '#09090b' },
  { id: 'sunset-amber', name: 'Amber Grid', bg: 'bg-neutral-950', text: 'text-neutral-100', cardBg: 'bg-amber-950/30', border: 'border-amber-900/40', accent: 'amber', accentBg: 'bg-amber-500', accentText: 'text-amber-400', headerBg: 'bg-neutral-900/80', previewBg: '#0a0a0a' },
  { id: 'slate-corporate', name: 'Slate Minimal', bg: 'bg-slate-900', text: 'text-slate-100', cardBg: 'bg-slate-800/60', border: 'border-slate-700/60', accent: 'sky', accentBg: 'bg-sky-500', accentText: 'text-sky-400', headerBg: 'bg-slate-900/90', previewBg: '#0f172a' },
  { id: 'monochrome-pro', name: 'Monochrome High-Contrast', bg: 'bg-black', text: 'text-white', cardBg: 'bg-zinc-900', border: 'border-zinc-700', accent: 'white', accentBg: 'bg-white', accentText: 'text-white', headerBg: 'bg-zinc-950', previewBg: '#000000' },
  { id: 'nordic-frost', name: 'Nordic Frost Light', bg: 'bg-sky-50', text: 'text-slate-900', cardBg: 'bg-white/90', border: 'border-sky-200', accent: 'sky', accentBg: 'bg-sky-600', accentText: 'text-sky-700', headerBg: 'bg-sky-100/80', previewBg: '#f0f9ff' },
  { id: 'crimson-utility', name: 'Crimson Alert', bg: 'bg-stone-950', text: 'text-stone-100', cardBg: 'bg-rose-950/30', border: 'border-rose-900/40', accent: 'rose', accentBg: 'bg-rose-600', accentText: 'text-rose-400', headerBg: 'bg-stone-900/80', previewBg: '#0c0a09' },
  { id: 'deep-ocean', name: 'Deep Ocean', bg: 'bg-blue-950', text: 'text-blue-50', cardBg: 'bg-blue-900/40', border: 'border-blue-800/50', accent: 'cyan', accentBg: 'bg-cyan-400', accentText: 'text-cyan-300', headerBg: 'bg-blue-950/90', previewBg: '#172554' },
  { id: 'forest-eco', name: 'Eco Bio-Light', bg: 'bg-emerald-50', text: 'text-emerald-950', cardBg: 'bg-white', border: 'border-emerald-200', accent: 'emerald', accentBg: 'bg-emerald-700', accentText: 'text-emerald-700', headerBg: 'bg-emerald-100/70', previewBg: '#ecfdf5' },
  { id: 'synthwave-80s', name: 'Neon Synthwave', bg: 'bg-slate-950', text: 'text-pink-100', cardBg: 'bg-fuchsia-950/30', border: 'border-pink-800/40', accent: 'pink', accentBg: 'bg-pink-500', accentText: 'text-pink-400', headerBg: 'bg-slate-900/90', previewBg: '#020617' },
  { id: 'warm-paper', name: 'Warm Cream Light', bg: 'bg-amber-50/50', text: 'text-amber-950', cardBg: 'bg-white', border: 'border-amber-200', accent: 'orange', accentBg: 'bg-amber-700', accentText: 'text-amber-800', headerBg: 'bg-amber-100/60', previewBg: '#fffbeb' },
  { id: 'obsidian-gold', name: 'Obsidian Gold', bg: 'bg-stone-950', text: 'text-amber-100', cardBg: 'bg-stone-900/80', border: 'border-amber-800/40', accent: 'yellow', accentBg: 'bg-amber-500', accentText: 'text-amber-300', headerBg: 'bg-stone-900', previewBg: '#0c0a09' },
  { id: 'dracula-dark', name: 'Dracula Dark', bg: 'bg-slate-900', text: 'text-purple-100', cardBg: 'bg-slate-800/80', border: 'border-indigo-800/50', accent: 'indigo', accentBg: 'bg-indigo-500', accentText: 'text-indigo-300', headerBg: 'bg-slate-950', previewBg: '#0f172a' }
];

const TENANTS = [
  { id: 'metro-elec', name: 'Metro Electric Grid', type: 'Electricity', icon: Zap, color: 'text-amber-400', activeCount: '1.2M Meters' },
  { id: 'apex-water', name: 'Apex Water & Sewer Authority', type: 'Water/Sewer', icon: Droplets, color: 'text-cyan-400', activeCount: '850K Accounts' },
  { id: 'national-gas', name: 'National Gas Pipeline Co', type: 'Gas', icon: Flame, color: 'text-orange-400', activeCount: '2.1M Connection' }
];

const INITIAL_TRANSACTIONS = [
  { id: 'TXN-984210', tenant: 'Metro Electric Grid', account: 'E-4819-2041', utility: 'Electricity', amount: '$142.50', status: 'SETTLED', gateway: 'Stripe Direct', latency: '8ms', idempotencyKey: 'idmp_98a4d1f2b3e4', timestamp: 'Just now', score: 0.99 },
  { id: 'TXN-984209', tenant: 'Apex Water & Sewer', account: 'W-9921-0021', utility: 'Water', amount: '$68.20', status: 'CLEARED', gateway: 'Plaid ACH', latency: '11ms', idempotencyKey: 'idmp_38f2a1b9e0d1', timestamp: '2s ago', score: 0.98 },
  { id: 'TXN-984208', tenant: 'National Gas Pipeline', account: 'G-1029-4412', utility: 'Gas', amount: '$210.00', status: 'SETTLED', gateway: 'Razorpay', latency: '6ms', idempotencyKey: 'idmp_77c1d3e8f9a2', timestamp: '5s ago', score: 1.00 },
  { id: 'TXN-984207', tenant: 'Metro Electric Grid', account: 'E-3312-9811', utility: 'Electricity', amount: '$89.00', status: 'PENDING', gateway: 'Stripe Direct', latency: '14ms', idempotencyKey: 'idmp_11e3b5c7d9a0', timestamp: '8s ago', score: 0.95 },
  { id: 'TXN-984206', tenant: 'Apex Water & Sewer', account: 'S-7721-3901', utility: 'Sewer', amount: '$45.75', status: 'RECONCILED', gateway: 'JPMC ACH', latency: '9ms', idempotencyKey: 'idmp_55a9c1d2e3f4', timestamp: '12s ago', score: 0.99 },
  { id: 'TXN-984205', tenant: 'National Gas Pipeline', account: 'G-8812-0043', utility: 'Gas', amount: '$312.40', status: 'SETTLED', gateway: 'Stripe Direct', latency: '7ms', idempotencyKey: 'idmp_22d4e6f8a0b1', timestamp: '15s ago', score: 0.97 },
  { id: 'TXN-984204', tenant: 'Metro Electric Grid', account: 'E-9012-3321', utility: 'Electricity', amount: '$520.10', status: 'CLEARED', gateway: 'Plaid ACH', latency: '10ms', idempotencyKey: 'idmp_88f0a2b4c6d8', timestamp: '18s ago', score: 0.98 },
];

const PRESET_PROMPTS = [
  "Analyze peak billing anomalies for Metro Electric Grid past 24 hours",
  "Audit failed card transactions & check payment gateway latencies",
  "Check compliance and tax calculation rules for Apex Water",
  "Identify accounts with sudden 300%+ consumption surges"
];

const OWASP_CONTROLS = [
  { id: 'A01:2021', name: 'Broken Access Control', status: 'PASSED', detail: 'Tenant-level RBAC enforced via JWT claims & context isolation.' },
  { id: 'A02:2021', name: 'Cryptographic Failures', status: 'PASSED', detail: 'TLS 1.3 in transit, AES-256 for token storage & zero raw PAN storage.' },
  { id: 'A03:2021', name: 'Injection (SQL/NoSQL/XSS)', status: 'PASSED', detail: 'Strict Zod schema parsing & React automatic DOM string escaping.' },
  { id: 'A04:2021', name: 'Insecure Design', status: 'PASSED', detail: 'Idempotency keys on all write operations to prevent duplicate payment replay.' },
  { id: 'A05:2021', name: 'Security Misconfiguration', status: 'PASSED', detail: 'CORS policy restricted, secure CSP headers, safe theme switching.' },
  { id: 'A07:2021', name: 'Identification & Auth Failures', status: 'PASSED', detail: 'Passkey biometric verification & session token rotation.' },
  { id: 'A08:2021', name: 'Software & Data Integrity', status: 'PASSED', detail: 'Package integrity verified, signed JWT payloads.' },
  { id: 'A09:2021', name: 'Security Logging & Monitoring', status: 'PASSED', detail: 'Real-time RediSearch stream logging for immediate audit capability.' }
];

export default function EnterprisePlatformDemo() {
  // Theme State (Default: Cyber Dark)
  const [currentTheme, setCurrentTheme] = useState(THEMES[0]);
  const [isThemeModalOpen, setIsThemeModalOpen] = useState(false);

  // Global & Navigation State
  const [activeTab, setActiveTab] = useState('observability');
  const [selectedTenant, setSelectedTenant] = useState(TENANTS[0]);
  const [isLive, setIsLive] = useState(true);
  const [tps, setTps] = useState(4820);
  const [avgLatency, setAvgLatency] = useState(8.4);
  const [successRate] = useState(99.982);

  // Time Series Chart Data
  const [chartData, setChartData] = useState([
    { time: '12:00', tps: 4200, latency: 9.1, mongodb: 3800, redis: 4200 },
    { time: '12:01', tps: 4450, latency: 8.8, mongodb: 4000, redis: 4450 },
    { time: '12:02', tps: 4300, latency: 9.5, mongodb: 3900, redis: 4300 },
    { time: '12:03', tps: 4800, latency: 8.2, mongodb: 4200, redis: 4800 },
    { time: '12:04', tps: 5100, latency: 7.9, mongodb: 4500, redis: 5100 },
    { time: '12:05', tps: 4950, latency: 8.4, mongodb: 4350, redis: 4950 },
  ]);

  // OLTP Transactions State
  const [transactions, setTransactions] = useState(INITIAL_TRANSACTIONS);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [selectedTxn, setSelectedTxn] = useState(null);

  // Agentic AI Engine State
  const [chatMessages, setChatMessages] = useState([
    {
      sender: 'assistant',
      text: "Greetings. I am the **LangEngine Agentic Copilot**. I monitor real-time RediSearch OLAP streams and MongoDB OLTP ledgers for anomaly detection, automated bill dispute resolutions, and OWASP-compliant payment integrity.",
      traces: null
    }
  ]);
  const [userInput, setUserInput] = useState('');
  const [isAgentThinking, setIsAgentThinking] = useState(false);

  // Mobile App Simulator State
  const [mobileUtility, setMobileUtility] = useState('Electricity');
  const [mobileAmount] = useState('142.50');
  const [mobileState, setMobileState] = useState('IDLE');
  const [isOfflineMode, setIsOfflineMode] = useState(false);
  const [biometricsApproved, setBiometricsApproved] = useState(false);

  // OWASP Input Sanitization Safeguard for live inputs
  const sanitizeInput = (str) => {
    return str.replace(/[<>&"'`]/g, (match) => {
      const escapeMap = { '<': '&lt;', '>': '&gt;', '&': '&amp;', '"': '&quot;', "'": '&#x27;', '`': '&#x60;' };
      return escapeMap[match];
    });
  };

  useEffect(() => {
    if (!isLive) return;

    const interval = setInterval(() => {
      // Fluctuate stats
      const newTps = Math.floor(4500 + Math.random() * 800);
      const newLatency = parseFloat((7.5 + Math.random() * 2.5).toFixed(1));
      setTps(newTps);
      setAvgLatency(newLatency);

      // Update Chart Data
      const now = new Date();
      const timeStr = `${now.getHours().toString().padStart(2, '0')}:${now.getMinutes().toString().padStart(2, '0')}:${now.getSeconds().toString().padStart(2, '0')}`;
      
      setChartData(prev => [
        ...prev.slice(-14),
        {
          time: timeStr,
          tps: newTps,
          latency: newLatency,
          mongodb: Math.floor(newTps * 0.88),
          redis: newTps
        }
      ]);

      // Randomly inject new incoming transaction
      if (Math.random() > 0.4) {
        const utilities = ['Electricity', 'Water', 'Gas', 'Sewer'];
        const statuses = ['SETTLED', 'CLEARED', 'CLEARED', 'SETTLED', 'PENDING'];
        const gateways = ['Stripe Direct', 'Plaid ACH', 'JPMC ACH', 'Razorpay'];
        const randomUtil = utilities[Math.floor(Math.random() * utilities.length)];
        const randomAmt = (20 + Math.random() * 300).toFixed(2);
        
        const newTxn = {
          id: `TXN-${Math.floor(900000 + Math.random() * 99999)}`,
          tenant: selectedTenant.name,
          account: `${randomUtil[0]}-${Math.floor(1000 + Math.random() * 9000)}-${Math.floor(1000 + Math.random() * 9000)}`,
          utility: randomUtil,
          amount: `$${randomAmt}`,
          status: statuses[Math.floor(Math.random() * statuses.length)],
          gateway: gateways[Math.floor(Math.random() * gateways.length)],
          latency: `${Math.floor(5 + Math.random() * 8)}ms`,
          idempotencyKey: `idmp_${Math.random().toString(36).substring(2, 14)}`,
          timestamp: 'Just now',
          score: parseFloat((0.95 + Math.random() * 0.05).toFixed(2))
        };

        setTransactions(prev => [newTxn, ...prev.slice(0, 19)]);
      }
    }, 2000);

    return () => clearInterval(interval);
  }, [isLive, selectedTenant]);

  const handleSendPrompt = (promptText) => {
    const textToSend = promptText || userInput;
    if (!textToSend.trim()) return;

    // Sanitize user text to prevent XSS / Injection injection into chat logs
    const sanitizedText = sanitizeInput(textToSend);

    setChatMessages(prev => [...prev, { sender: 'user', text: sanitizedText }]);
    setUserInput('');
    setIsAgentThinking(true);

    setTimeout(() => {
      let agentResponse = "";
      let traces = [];

      if (textToSend.toLowerCase().includes('anomaly') || textToSend.toLowerCase().includes('metro electric')) {
        traces = [
          { step: 'Ingestion & Intent', detail: 'Identified entity: Metro Electric Grid. Query type: Billing Anomaly & Meter Surge.' },
          { step: 'RediSearch Vector Query', detail: 'Queried vector index `idx:utility_meter_logs` using HNSW cosine distance.' },
          { step: 'MongoDB OLTP Cross-Ref', detail: 'Fetched historical baseline for 12,400 meters in Zone 4.' },
          { step: 'OWASP Sanitization Check', detail: 'Validated query string against OWASP A03 SQLi/NoSQLi signatures.' }
        ];
        agentResponse = `**Audit Completed for Metro Electric Grid:**\n\nI scanned **14,280 recent smart meter payloads** across RediSearch OLAP indexes. \n\n* **Identified Anomaly**: Account **E-4819-2041** experienced a **410% spike** at 11:42 AM IST due to a firmware handshake retry loop on Meter Node #882.\n* **Action Triggered**: Applied automatic holding status to pending charge **$142.50**. Generated Zod schema validation flag for engineering review.`;
      } else if (textToSend.toLowerCase().includes('failed') || textToSend.toLowerCase().includes('card') || textToSend.toLowerCase().includes('latenc')) {
        traces = [
          { step: 'RediSearch Stream Lookup', detail: 'Aggregated error logs from `stream:gateway_telemetry` over past 60m.' },
          { step: 'Gateway Correlation', detail: 'Plaid ACH (99.9% health), Stripe Direct (99.8% health), JPMC ACH (98.2% - transient timeout).' },
          { step: 'Mitigation Execution', detail: 'Rerouted 12% high-value card settlements to secondary gateway endpoint.' }
        ];
        agentResponse = `**Gateway Latency & Failure Report:**\n\n* **Average End-to-End Latency**: **8.4ms** (Target: &lt;15ms).\n* **Primary Gateway Health**: All payment rails operational. \n* **Recommendation**: JPMC ACH gateway experienced a minor 42ms jitter window 10 minutes ago. Automatic fallback logic successfully rerouted 14 transactions with **0% loss**.`;
      } else {
        traces = [
          { step: 'RAG Retrieval', detail: 'Retrieved utility tax policies for state jurisdiction #0912.' },
          { step: 'OWASP Schema Enforcement', detail: 'Validated payload against Enterprise Utility Zod Contract v3.2 & OWASP Top 10 rules.' }
        ];
        agentResponse = `**System Status & Compliance Check:**\n\nAll system layers (MongoDB Replica Cluster, RediSearch Vector Node, OAuth 2.0 Auth Server) are operating within optimal parameters with **99.982% SLA availability**. Multi-tenant data isolation and OWASP Security Controls are actively enforced.`;
      }

      setChatMessages(prev => [
        ...prev,
        { sender: 'assistant', text: agentResponse, traces }
      ]);
      setIsAgentThinking(false);
    }, 1800);
  };

  const handleMobilePay = () => {
    if (!biometricsApproved) {
      alert("Security Notice: Please authenticate using Biometric Passkey / Fingerprint simulator.");
      return;
    }
    setMobileState('PROCESSING');
    setTimeout(() => {
      setMobileState('SUCCESS');
      const newTxn = {
        id: `TXN-${Math.floor(900000 + Math.random() * 99999)}`,
        tenant: selectedTenant.name,
        account: `MOB-${Math.floor(1000 + Math.random() * 9000)}`,
        utility: mobileUtility,
        amount: `$${mobileAmount}`,
        status: isOfflineMode ? 'PENDING (Offline Queue)' : 'CLEARED',
        gateway: 'Mobile Android SDK (Passkey)',
        latency: isOfflineMode ? 'Sync Queued' : '6ms',
        idempotencyKey: `mob_idmp_${Math.random().toString(36).substring(2, 10)}`,
        timestamp: 'Just now',
        score: 1.00
      };
      setTransactions(prev => [newTxn, ...prev]);
    }, 1500);
  };

  return (
    <div className={`min-h-screen ${currentTheme.bg} ${currentTheme.text} font-sans antialiased flex flex-col transition-colors duration-300`}>
      
      {/* ================= EXECUTIVE TOP HEADER ================= */}
      <header className={`border-b ${currentTheme.border} ${currentTheme.headerBg} backdrop-blur-md sticky top-0 z-50 px-4 lg:px-8 py-3 flex flex-wrap items-center justify-between gap-4`}>
        
        {/* Brand & Platform Identity */}
        <div className="flex items-center gap-3">
          <div className={`${currentTheme.accentBg} p-2.5 rounded-xl shadow-lg ring-1 ring-white/20 text-white`}>
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="font-bold text-lg tracking-wide">GRIDFLOW</h1>
              <span className={`text-[10px] ${currentTheme.cardBg} ${currentTheme.accentText} border ${currentTheme.border} px-2 py-0.5 rounded-full font-mono uppercase tracking-wider`}>
                OWASP Compliant • Enterprise v4.2
              </span>
            </div>
            <p className="text-xs opacity-70">Utility Payment SaaS Platform • OLTP & Real-Time OLAP</p>
          </div>
        </div>

        {/* Tenant Switcher & Global Context */}
        <div className={`flex items-center gap-3 ${currentTheme.cardBg} border ${currentTheme.border} rounded-xl p-1.5 pl-3`}>
          <span className="text-xs opacity-70 flex items-center gap-1.5 font-medium">
            <Building2 className="w-3.5 h-3.5" /> Active Tenant:
          </span>
          <div className="flex gap-1">
            {TENANTS.map(t => {
              const Icon = t.icon;
              const isSelected = selectedTenant.id === t.id;
              return (
                <button
                  key={t.id}
                  onClick={() => setSelectedTenant(t)}
                  className={`flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                    isSelected 
                      ? `${currentTheme.accentBg} text-white shadow-sm` 
                      : 'opacity-70 hover:opacity-100 hover:bg-black/10'
                  }`}
                >
                  <Icon className={`w-3.5 h-3.5 ${t.color}`} />
                  <span>{t.name.split(' ')[0]}</span>
                </button>
              );
            })}
          </div>
        </div>

        {/* Live Metrics Header Gauges, Theme Switcher & Control */}
        <div className="flex items-center gap-3">
          
          {/* THEME SELECTOR BUTTON */}
          <button
            onClick={() => setIsThemeModalOpen(true)}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all border ${currentTheme.border} ${currentTheme.cardBg} hover:opacity-90 shadow-sm`}
          >
            <Palette className={`w-4 h-4 ${currentTheme.accentText}`} />
            <span className="hidden sm:inline">Theme: {currentTheme.name}</span>
            <span className="sm:hidden">Theme</span>
          </button>

          {/* Live TPS Counter */}
          <div className={`hidden md:flex items-center gap-3 ${currentTheme.cardBg} border ${currentTheme.border} px-3.5 py-1.5 rounded-xl`}>
            <div className="relative flex h-2.5 w-2.5">
              <span className={`animate-ping absolute inline-flex h-full w-full rounded-full opacity-75 ${isLive ? 'bg-emerald-400' : 'bg-amber-400'}`}></span>
              <span className={`relative inline-flex rounded-full h-2.5 w-2.5 ${isLive ? 'bg-emerald-500' : 'bg-amber-500'}`}></span>
            </div>
            <div>
              <div className="text-[10px] uppercase tracking-wider opacity-60 font-medium">Throughput</div>
              <div className="text-sm font-mono font-bold flex items-center gap-1">
                {tps.toLocaleString()} <span className="text-[10px] opacity-60 font-sans">TPS</span>
              </div>
            </div>
          </div>

          {/* Toggle Live Feed Simulation */}
          <button
            onClick={() => setIsLive(!isLive)}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-xl text-xs font-bold transition-all border ${
              isLive 
                ? 'bg-emerald-950/40 border-emerald-800/80 text-emerald-400' 
                : 'bg-amber-950/40 border-amber-800/80 text-amber-400'
            }`}
          >
            {isLive ? <Pause className="w-3.5 h-3.5" /> : <Play className="w-3.5 h-3.5" />}
            <span className="hidden sm:inline">{isLive ? 'Pause Stream' : 'Resume'}</span>
          </button>

        </div>

      </header>

      {/* ================= NAVIGATION TABS ================= */}
      <nav className={`border-b ${currentTheme.border} ${currentTheme.cardBg} px-4 lg:px-8 flex items-center overflow-x-auto no-scrollbar gap-2 py-2`}>
        {[
          { id: 'observability', label: 'Real-Time Observability', icon: Activity, badge: 'RediSearch OLAP' },
          { id: 'transactions', label: 'Transactions & Ledger', icon: Database, badge: 'MongoDB OLTP' },
          { id: 'owasp-security', label: 'OWASP Security Suite', icon: ShieldCheck, badge: 'Compliance' },
          { id: 'ai-assistant', label: 'Agentic Copilot', icon: Bot, badge: 'LangGraph' },
          { id: 'mobile-app', label: 'Android Pay Simulator', icon: Smartphone, badge: 'React Native' },
          { id: 'architecture', label: 'System Topology', icon: Layers, badge: 'Architecture' }
        ].map(tab => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2.5 px-4 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all ${
                isActive 
                  ? `${currentTheme.accentBg} text-white shadow-md` 
                  : 'opacity-70 hover:opacity-100 hover:bg-black/5'
              }`}
            >
              <Icon className="w-4 h-4" />
              <span>{tab.label}</span>
              <span className={`text-[10px] px-2 py-0.5 rounded-full font-mono ${
                isActive ? 'bg-black/20 text-white' : 'bg-black/10 opacity-70'
              }`}>
                {tab.badge}
              </span>
            </button>
          );
        })}
      </nav>

      {/* ================= MAIN DASHBOARD BODY ================= */}
      <main className="flex-1 p-4 lg:p-8 max-w-[1600px] w-full mx-auto space-y-6">

        {activeTab === 'observability' && (
          <div className="space-y-6 animate-fadeIn">
            
            {/* Top Stat Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              
              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-5 rounded-2xl relative overflow-hidden group transition-all`}>
                <div className="flex items-center justify-between opacity-70 text-xs mb-3">
                  <span className="font-semibold uppercase tracking-wider">OLAP Query Latency</span>
                  <Activity className={`w-4 h-4 ${currentTheme.accentText}`} />
                </div>
                <div className="text-3xl font-extrabold font-mono mb-2">{avgLatency} <span className="text-sm font-sans opacity-60">ms</span></div>
                <div className="flex items-center gap-1.5 text-xs text-emerald-500 font-medium">
                  <ArrowDownRight className="w-3.5 h-3.5" />
                  <span>Sub-15ms Target Met</span>
                </div>
              </div>

              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-5 rounded-2xl relative overflow-hidden group transition-all`}>
                <div className="flex items-center justify-between opacity-70 text-xs mb-3">
                  <span className="font-semibold uppercase tracking-wider">Gateway SLA Success</span>
                  <ShieldCheck className="w-4 h-4 text-emerald-500" />
                </div>
                <div className="text-3xl font-extrabold font-mono mb-2">{successRate}%</div>
                <div className="flex items-center gap-1.5 text-xs text-emerald-500 font-medium">
                  <CheckCircle2 className="w-3.5 h-3.5" />
                  <span>Zero settlement losses</span>
                </div>
              </div>

              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-5 rounded-2xl relative overflow-hidden group transition-all`}>
                <div className="flex items-center justify-between opacity-70 text-xs mb-3">
                  <span className="font-semibold uppercase tracking-wider">OWASP Threat Score</span>
                  <Lock className={`w-4 h-4 ${currentTheme.accentText}`} />
                </div>
                <div className="text-3xl font-extrabold font-mono mb-2">0.00 <span className="text-sm font-sans text-emerald-500 font-normal">Clean</span></div>
                <div className="flex items-center gap-1.5 text-xs text-emerald-500 font-medium">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  <span>Top 10 Mitigations Active</span>
                </div>
              </div>

              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-5 rounded-2xl relative overflow-hidden group transition-all`}>
                <div className="flex items-center justify-between opacity-70 text-xs mb-3">
                  <span className="font-semibold uppercase tracking-wider">Active Stream Consumers</span>
                  <HardDrive className="w-4 h-4 text-amber-500" />
                </div>
                <div className="text-3xl font-extrabold font-mono mb-2">64 <span className="text-sm font-sans opacity-60">Workers</span></div>
                <div className="flex items-center gap-1.5 text-xs opacity-70 font-medium">
                  <Server className="w-3.5 h-3.5 text-amber-500" />
                  <span>Redis Event Bus Streams</span>
                </div>
              </div>

            </div>

            {/* Main Interactive Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
              
              <div className={`lg:col-span-2 ${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl flex flex-col justify-between`}>
                <div className="flex flex-wrap items-center justify-between gap-4 mb-6">
                  <div>
                    <h2 className="text-base font-bold flex items-center gap-2">
                      <TrendingUp className={`w-4 h-4 ${currentTheme.accentText}`} /> Real-Time Payment Throughput (RediSearch vs MongoDB)
                    </h2>
                    <p className="text-xs opacity-70">Live stream metrics comparing incoming Redis events with MongoDB write commits</p>
                  </div>
                </div>

                <div className="h-72 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={chartData}>
                      <CartesianGrid strokeDasharray="3 3" opacity={0.1} />
                      <XAxis dataKey="time" stroke="currentColor" fontSize={11} opacity={0.5} />
                      <YAxis stroke="currentColor" fontSize={11} domain={[2000, 'dataMax + 1000']} opacity={0.5} />
                      <Tooltip 
                        contentStyle={{ backgroundColor: '#0f172a', borderColor: '#334155', borderRadius: '12px', color: '#f8fafc' }}
                        itemStyle={{ fontSize: '12px' }}
                      />
                      <Area type="monotone" dataKey="redis" stroke="#22d3ee" strokeWidth={2} fillOpacity={0.2} fill="#22d3ee" />
                      <Area type="monotone" dataKey="mongodb" stroke="#6366f1" strokeWidth={2} fillOpacity={0.2} fill="#6366f1" />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              </div>

              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl flex flex-col justify-between`}>
                <div>
                  <h2 className="text-base font-bold mb-1 flex items-center gap-2">
                    <CreditCard className="w-4 h-4 text-emerald-500" /> Gateway Volume Split
                  </h2>
                  <p className="text-xs opacity-70 mb-6">Payment rail processing distribution</p>

                  <div className="space-y-4">
                    {[
                      { name: 'Stripe Direct (Card)', share: '48%', color: 'bg-cyan-500', latency: '6ms' },
                      { name: 'Plaid ACH (Bank Sync)', share: '32%', color: 'bg-emerald-500', latency: '12ms' },
                      { name: 'JPMC Direct Ledger', share: '14%', color: 'bg-indigo-500', latency: '9ms' },
                      { name: 'Razorpay / Local SDK', share: '6%', color: 'bg-amber-500', latency: '14ms' }
                    ].map((gw, idx) => (
                      <div key={idx} className="space-y-1.5">
                        <div className="flex justify-between text-xs font-semibold">
                          <span>{gw.name}</span>
                          <span className="font-mono opacity-70">{gw.share} • Avg {gw.latency}</span>
                        </div>
                        <div className="h-2 w-full bg-black/10 rounded-full overflow-hidden">
                          <div className={`h-full ${gw.color} rounded-full`} style={{ width: gw.share }}></div>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>

                <div className={`mt-6 pt-4 border-t ${currentTheme.border} p-3 rounded-xl border`}>
                  <div className="flex items-center gap-2 text-xs opacity-80">
                    <ShieldCheck className="w-4 h-4 text-emerald-500 shrink-0" />
                    <span>PCI-DSS Tokenization & OWASP Input Validation active.</span>
                  </div>
                </div>
              </div>

            </div>

          </div>
        )}

        {activeTab === 'transactions' && (
          <div className="space-y-6 animate-fadeIn">
            
            {/* Filter Bar */}
            <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-4 rounded-2xl flex flex-wrap items-center justify-between gap-4`}>
              
              <div className="relative flex-1 min-w-[280px]">
                <Search className="w-4 h-4 opacity-50 absolute left-3.5 top-3" />
                <input
                  type="text"
                  placeholder="Search by Transaction ID, Account, or Key..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(sanitizeInput(e.target.value))}
                  className={`w-full bg-black/10 border ${currentTheme.border} rounded-xl pl-10 pr-4 py-2 text-xs text-inherit focus:outline-none transition-all`}
                />
              </div>

              <div className="flex items-center gap-1.5 bg-black/10 p-1 rounded-xl border border-white/10">
                {['ALL', 'SETTLED', 'CLEARED', 'PENDING'].map(st => (
                  <button
                    key={st}
                    onClick={() => setStatusFilter(st)}
                    className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                      statusFilter === st 
                        ? `${currentTheme.accentBg} text-white` 
                        : 'opacity-70 hover:opacity-100'
                    }`}
                  >
                    {st}
                  </button>
                ))}
              </div>

            </div>

            {/* High-Density Transaction Table */}
            <div className={`${currentTheme.cardBg} border ${currentTheme.border} rounded-2xl overflow-hidden shadow-xl`}>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-black/20 opacity-70 font-mono uppercase text-[10px] tracking-wider border-b border-white/10">
                    <tr>
                      <th className="py-3.5 px-4">Transaction ID</th>
                      <th className="py-3.5 px-4">Tenant Scope</th>
                      <th className="py-3.5 px-4">Account / Utility</th>
                      <th className="py-3.5 px-4">Amount</th>
                      <th className="py-3.5 px-4">Gateway Rail</th>
                      <th className="py-3.5 px-4">Status</th>
                      <th className="py-3.5 px-4">Idempotency Key</th>
                      <th className="py-3.5 px-4 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5 font-sans">
                    {transactions
                      .filter(t => statusFilter === 'ALL' || t.status.includes(statusFilter))
                      .filter(t => !searchTerm || t.id.toLowerCase().includes(searchTerm.toLowerCase()) || t.account.toLowerCase().includes(searchTerm.toLowerCase()))
                      .map((txn) => (
                        <tr key={txn.id} className="hover:bg-black/10 transition-colors">
                          <td className={`py-3.5 px-4 font-mono font-bold ${currentTheme.accentText}`}>{txn.id}</td>
                          <td className="py-3.5 px-4 font-medium">{txn.tenant}</td>
                          <td className="py-3.5 px-4">
                            <div className="font-medium">{txn.account}</div>
                            <div className="text-[10px] opacity-60">{txn.utility}</div>
                          </td>
                          <td className="py-3.5 px-4 font-mono font-bold">{txn.amount}</td>
                          <td className="py-3.5 px-4 opacity-80">{txn.gateway}</td>
                          <td className="py-3.5 px-4">
                            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-bold font-mono bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                              {txn.status}
                            </span>
                          </td>
                          <td className="py-3.5 px-4 font-mono text-[10px] opacity-60">{txn.idempotencyKey}</td>
                          <td className="py-3.5 px-4 text-right">
                            <button onClick={() => setSelectedTxn(txn)} className={`text-xs ${currentTheme.accentText} font-semibold hover:underline`}>
                              Inspect
                            </button>
                          </td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
            </div>

          </div>
        )}

        {activeTab === 'owasp-security' && (
          <div className="space-y-6 animate-fadeIn">
            
            <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl space-y-4`}>
              <div className="flex items-center justify-between">
                <div>
                  <h2 className="text-lg font-bold flex items-center gap-2">
                    <ShieldCheck className="w-5 h-5 text-emerald-500" /> OWASP Top 10 Security Controls Audit Matrix
                  </h2>
                  <p className="text-xs opacity-70">Automated verification of application security controls against OWASP 2021 standards.</p>
                </div>
                <span className="bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 px-3 py-1 rounded-full text-xs font-mono font-bold">
                  Compliance Score: 100%
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-4">
                {OWASP_CONTROLS.map((control) => (
                  <div key={control.id} className={`p-4 rounded-xl border ${currentTheme.border} bg-black/10 space-y-2`}>
                    <div className="flex items-center justify-between">
                      <span className="font-mono text-xs font-bold text-emerald-400">{control.id}</span>
                      <span className="text-[10px] bg-emerald-950 text-emerald-400 border border-emerald-800 px-2 py-0.5 rounded-full font-bold">
                        {control.status}
                      </span>
                    </div>
                    <div className="text-xs font-bold">{control.name}</div>
                    <p className="text-[11px] opacity-70">{control.detail}</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              
              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl space-y-3`}>
                <h3 className="text-sm font-bold flex items-center gap-2">
                  <Lock className="w-4 h-4 text-cyan-400" /> Input Sanitization & XSS Defense Engine
                </h3>
                <p className="text-xs opacity-70">
                  All payment payloads, account queries, and AI chatbot inputs are sanitized using strict HTML entity escaping and validated via Zod schemas prior to DB ingestion.
                </p>
                <div className="p-3 bg-black/30 rounded-xl font-mono text-xs opacity-90 space-y-1">
                  <div className="text-slate-400">// Example Input Escaping Rule</div>
                  <div className="text-cyan-400">const sanitizeInput = (str) =&gt; &#123;</div>
                  <div className="pl-4 text-amber-300">return str.replace(/[&lt;&gt;&amp;"'`]/g, (match) =&gt; escapeMap[match]);</div>
                  <div className="text-cyan-400">&#125;;</div>
                </div>
              </div>

              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl space-y-3`}>
                <h3 className="text-sm font-bold flex items-center gap-2">
                  <KeyIcon className="w-4 h-4 text-indigo-400" /> Rate Limiting & Idempotency Safeguard
                </h3>
                <p className="text-xs opacity-70">
                  To mitigate OWASP A04 (Insecure Design) and Replay Attacks, every transactional API request requires an `idempotencyKey` stored in Redis with an atomic 24-hour expiration window.
                </p>
                <div className="flex items-center gap-3 pt-2 font-mono text-xs">
                  <div className="p-2.5 bg-black/20 rounded-lg border border-white/10 text-emerald-400">
                    ✓ Replay Protection Active
                  </div>
                  <div className="p-2.5 bg-black/20 rounded-lg border border-white/10 text-emerald-400">
                    ✓ CORS Strict Headers Enforced
                  </div>
                </div>
              </div>

            </div>

          </div>
        )}

        {activeTab === 'ai-assistant' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fadeIn">
            
            <div className={`lg:col-span-2 ${currentTheme.cardBg} border ${currentTheme.border} rounded-2xl flex flex-col h-[640px]`}>
              
              <div className={`p-4 border-b ${currentTheme.border} bg-black/20 rounded-t-2xl flex items-center justify-between`}>
                <div className="flex items-center gap-3">
                  <div className={`${currentTheme.accentBg} p-2 rounded-xl text-white`}>
                    <Bot className="w-5 h-5" />
                  </div>
                  <div>
                    <h2 className="text-sm font-bold">LangEngine Copilot (OWASP Guarded)</h2>
                    <p className="text-[10px] opacity-70">Powered by LangGraph & OWASP Input Filtering</p>
                  </div>
                </div>
              </div>

              <div className="flex-1 p-4 overflow-y-auto space-y-4 no-scrollbar">
                {chatMessages.map((msg, idx) => (
                  <div key={idx} className={`flex flex-col ${msg.sender === 'user' ? 'items-end' : 'items-start'}`}>
                    <div className={`max-w-[85%] p-4 rounded-2xl text-xs space-y-2 shadow-lg ${
                      msg.sender === 'user'
                        ? `${currentTheme.accentBg} text-white rounded-br-none`
                        : `${currentTheme.cardBg} border ${currentTheme.border} rounded-bl-none`
                    }`}>
                      <div className="whitespace-pre-wrap leading-relaxed">{msg.text}</div>

                      {msg.traces && (
                        <div className="mt-3 pt-3 border-t border-white/10 space-y-2">
                          <div className={`text-[10px] font-mono ${currentTheme.accentText} uppercase tracking-wider font-bold`}>
                            Agent Execution Traces (LangGraph Pipeline)
                          </div>
                          <div className="space-y-1.5 font-mono text-[11px]">
                            {msg.traces.map((tr, tIdx) => (
                              <div key={tIdx} className="bg-black/20 p-2 rounded-lg border border-white/5 flex items-start gap-2">
                                <span className={`${currentTheme.accentText} font-bold`}>{tIdx + 1}.</span>
                                <div>
                                  <span className="font-semibold">{tr.step}: </span>
                                  <span className="opacity-70">{tr.detail}</span>
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                ))}

                {isAgentThinking && (
                  <div className={`flex items-center gap-2 text-xs ${currentTheme.accentText} font-mono animate-pulse bg-black/20 p-3 rounded-xl border ${currentTheme.border} w-fit`}>
                    <Sparkles className="w-4 h-4 animate-spin" />
                    <span>Executing agent reasoning with OWASP sanitization pipeline...</span>
                  </div>
                )}
              </div>

              <div className={`p-4 border-t ${currentTheme.border} bg-black/20 rounded-b-2xl`}>
                <form onSubmit={(e) => { e.preventDefault(); handleSendPrompt(); }} className="flex items-center gap-2">
                  <input
                    type="text"
                    placeholder="Ask agent to audit transactions, check OWASP security logs, or resolve billing disputes..."
                    value={userInput}
                    onChange={(e) => setUserInput(e.target.value)}
                    className={`flex-1 bg-black/20 border ${currentTheme.border} rounded-xl px-4 py-2.5 text-xs text-inherit placeholder-slate-400 focus:outline-none transition-all`}
                  />
                  <button type="submit" disabled={isAgentThinking} className={`${currentTheme.accentBg} text-white font-bold px-4 py-2.5 rounded-xl text-xs transition-all flex items-center gap-1.5 shadow-lg`}>
                    <Send className="w-3.5 h-3.5" />
                    <span>Run Agent</span>
                  </button>
                </form>
              </div>

            </div>

            <div className="space-y-6">
              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-5 rounded-2xl space-y-3`}>
                <h3 className="text-xs font-bold uppercase tracking-wider flex items-center gap-2">
                  <Sparkles className={`w-4 h-4 ${currentTheme.accentText}`} /> Trigger Agent Presets
                </h3>
                <div className="space-y-2">
                  {PRESET_PROMPTS.map((prompt, idx) => (
                    <button
                      key={idx}
                      onClick={() => handleSendPrompt(prompt)}
                      className={`w-full text-left p-3 rounded-xl bg-black/10 border ${currentTheme.border} hover:border-white/30 text-xs transition-all flex items-center justify-between group`}
                    >
                      <span>{prompt}</span>
                      <ChevronRight className="w-4 h-4 opacity-50 group-hover:opacity-100 transition-opacity shrink-0" />
                    </button>
                  ))}
                </div>
              </div>
            </div>

          </div>
        )}

        {activeTab === 'mobile-app' && (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-center justify-center animate-fadeIn py-4">
            
            <div className="mx-auto w-full max-w-[340px] bg-slate-950 border-4 border-slate-800 rounded-[40px] p-4 shadow-2xl relative text-slate-100">
              
              <div className="w-28 h-4 bg-slate-900 rounded-b-xl mx-auto mb-4 flex items-center justify-center">
                <div className="w-3 h-3 rounded-full bg-slate-950"></div>
              </div>

              <div className="bg-slate-900 rounded-[28px] p-4 space-y-5 border border-slate-800">
                
                <div className="flex items-center justify-between border-b border-slate-800 pb-3">
                  <div className="flex items-center gap-2">
                    <div className="w-7 h-7 rounded-lg bg-cyan-500 flex items-center justify-center text-slate-950 font-bold text-xs">⚡</div>
                    <div>
                      <div className="text-xs font-bold text-white">UtilityPay Mobile</div>
                      <div className="text-[9px] text-slate-400">Android Passkey App</div>
                    </div>
                  </div>
                  
                  <button 
                    onClick={() => setIsOfflineMode(!isOfflineMode)}
                    className={`flex items-center gap-1 px-2 py-0.5 rounded-full text-[9px] font-bold ${
                      isOfflineMode ? 'bg-amber-950 text-amber-400 border border-amber-800' : 'bg-emerald-950 text-emerald-400 border border-emerald-800'
                    }`}
                  >
                    {isOfflineMode ? <WifiOff className="w-3 h-3" /> : <Wifi className="w-3 h-3" />}
                    <span>{isOfflineMode ? 'Offline' : 'Online'}</span>
                  </button>
                </div>

                <div className="bg-gradient-to-br from-cyan-950/80 to-slate-950 p-4 rounded-2xl border border-cyan-800/40 space-y-2">
                  <div className="text-[10px] text-cyan-300 font-semibold uppercase tracking-wider">Current Utility Bill</div>
                  <div className="text-2xl font-extrabold text-white">${mobileAmount}</div>
                  <div className="text-[10px] text-slate-400">Account: E-4819-2041 (Metro Electric)</div>
                </div>

                <div className="p-3 bg-slate-950 rounded-xl border border-slate-800 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Fingerprint className={`w-5 h-5 ${biometricsApproved ? 'text-emerald-400' : 'text-slate-500'}`} />
                    <span className="text-[11px] text-slate-300">Biometric Passkey</span>
                  </div>
                  <button
                    onClick={() => setBiometricsApproved(!biometricsApproved)}
                    className={`px-2.5 py-1 rounded-lg text-[10px] font-bold transition-all ${
                      biometricsApproved ? 'bg-emerald-950 text-emerald-400 border border-emerald-800' : 'bg-slate-800 text-slate-300'
                    }`}
                  >
                    {biometricsApproved ? 'Verified' : 'Authenticate'}
                  </button>
                </div>

                {mobileState === 'SUCCESS' ? (
                  <div className="p-4 bg-emerald-950/60 border border-emerald-800/80 rounded-2xl text-center space-y-2">
                    <CheckCircle2 className="w-8 h-8 text-emerald-400 mx-auto" />
                    <div className="text-xs font-bold text-emerald-300">
                      {isOfflineMode ? 'Payment Queued Offline' : 'Payment Cleared Instant'}
                    </div>
                    <button onClick={() => setMobileState('IDLE')} className="text-[10px] text-cyan-400 hover:underline font-bold mt-2">
                      Make Another Payment
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={handleMobilePay}
                    disabled={mobileState === 'PROCESSING'}
                    className="w-full bg-cyan-500 hover:bg-cyan-400 text-slate-950 font-bold py-3 rounded-xl text-xs transition-all flex items-center justify-center gap-2"
                  >
                    <CreditCard className="w-4 h-4" />
                    <span>Pay ${mobileAmount} Now</span>
                  </button>
                )}

              </div>
            </div>

            <div className="space-y-6">
              <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl space-y-4`}>
                <h2 className="text-base font-bold flex items-center gap-2">
                  <Smartphone className={`w-5 h-5 ${currentTheme.accentText}`} /> Offline-First & OWASP Protected Mobile Client
                </h2>
                <p className="text-xs opacity-80 leading-relaxed">
                  Utility consumers frequently experience weak cellular connectivity in rural meter zones. The mobile client features an offline transaction store with encrypted local MMKV cache and biometric authentication.
                </p>
              </div>
            </div>

          </div>
        )}

        {activeTab === 'architecture' && (
          <div className="space-y-6 animate-fadeIn">
            
            <div className={`${currentTheme.cardBg} border ${currentTheme.border} p-6 rounded-2xl space-y-6`}>
              <div>
                <h2 className="text-base font-bold flex items-center gap-2">
                  <Layers className={`w-5 h-5 ${currentTheme.accentText}`} /> Decoupled Enterprise MERN + RediSearch Topology
                </h2>
                <p className="text-xs opacity-70">High-throughput isolation separating transactional OLTP from real-time analytics and OWASP compliance auditing</p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-4 gap-4 py-4">
                <div className={`bg-black/10 border ${currentTheme.border} p-4 rounded-2xl space-y-3 text-center`}>
                  <Globe className={`w-6 h-6 mx-auto ${currentTheme.accentText}`} />
                  <div className="text-xs font-bold">Client Tier</div>
                  <div className="text-[10px] opacity-60">Next.js Web Admin + React Native</div>
                </div>

                <div className={`bg-black/10 border ${currentTheme.border} p-4 rounded-2xl space-y-3 text-center`}>
                  <Lock className="w-6 h-6 mx-auto text-emerald-500" />
                  <div className="text-xs font-bold">OWASP Gateway</div>
                  <div className="text-[10px] opacity-60">JWT Auth + Input Sanitization</div>
                </div>

                <div className={`bg-black/10 border ${currentTheme.border} p-4 rounded-2xl space-y-3 text-center`}>
                  <Database className="w-6 h-6 mx-auto text-indigo-500" />
                  <div className="text-xs font-bold">MongoDB (OLTP)</div>
                  <div className="text-[10px] opacity-60">Financial Ledger Source of Truth</div>
                </div>

                <div className={`bg-black/10 border ${currentTheme.border} p-4 rounded-2xl space-y-3 text-center`}>
                  <Bot className="w-6 h-6 mx-auto text-amber-500" />
                  <div className="text-xs font-bold">RediSearch + Copilot</div>
                  <div className="text-[10px] opacity-60">Sub-15ms Vector Stream Engine</div>
                </div>
              </div>
            </div>

          </div>
        )}

      </main>

      {/* ================= 15 THEME SELECTION MODAL ================= */}
      {isThemeModalOpen && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-md z-50 flex items-center justify-center p-4">
          <div className={`${currentTheme.cardBg} border ${currentTheme.border} rounded-2xl max-w-3xl w-full p-6 space-y-6 shadow-2xl animate-scaleUp max-h-[85vh] flex flex-col`}>
            
            <div className="flex items-center justify-between border-b border-white/10 pb-4">
              <div>
                <h3 className="text-lg font-bold flex items-center gap-2">
                  <Palette className={`w-5 h-5 ${currentTheme.accentText}`} /> Portal Theme Gallery (15 Options)
                </h3>
                <p className="text-xs opacity-70">Choose a theme customized for enterprise utility operational environments</p>
              </div>
              <button 
                onClick={() => setIsThemeModalOpen(false)}
                className="text-inherit opacity-60 hover:opacity-100 p-1 font-bold text-lg"
              >
                ✕
              </button>
            </div>

            {/* Grid of 15 Themes */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3 overflow-y-auto p-1 flex-1 no-scrollbar">
              {THEMES.map((theme) => {
                const isSelected = currentTheme.id === theme.id;
                return (
                  <button
                    key={theme.id}
                    onClick={() => {
                      setCurrentTheme(theme);
                      setIsThemeModalOpen(false);
                    }}
                    className={`p-3.5 rounded-xl border text-left transition-all relative space-y-2 flex flex-col justify-between ${
                      isSelected 
                        ? 'ring-2 ring-emerald-500 border-emerald-500 shadow-md' 
                        : 'border-white/10 hover:border-white/30'
                    }`}
                    style={{ backgroundColor: theme.previewBg }}
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-xs font-bold text-slate-100">{theme.name}</span>
                      {isSelected && <CheckCircle2 className="w-4 h-4 text-emerald-400" />}
                    </div>

                    <div className="flex gap-1.5 pt-2">
                      <span className={`w-4 h-4 rounded-full ${theme.accentBg}`}></span>
                      <span className="w-4 h-4 rounded-full bg-slate-700"></span>
                      <span className="w-4 h-4 rounded-full bg-slate-900"></span>
                    </div>
                  </button>
                );
              })}
            </div>

            <div className="border-t border-white/10 pt-4 flex justify-between items-center text-xs opacity-70">
              <span>All 15 themes strictly maintain OWASP & WCAG AA color contrast standards.</span>
              <button
                onClick={() => setIsThemeModalOpen(false)}
                className={`${currentTheme.accentBg} text-white px-4 py-2 rounded-xl font-bold`}
              >
                Done
              </button>
            </div>

          </div>
        </div>
      )}

      {/* Modal Inspector for Transactions */}
      {selectedTxn && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className={`${currentTheme.cardBg} border ${currentTheme.border} rounded-2xl max-w-xl w-full p-6 space-y-4 shadow-2xl`}>
            <div className="flex items-center justify-between border-b border-white/10 pb-3">
              <h3 className="text-base font-bold font-mono">OWASP Payload & Zod Validation Inspector</h3>
              <button onClick={() => setSelectedTxn(null)} className="opacity-60 hover:opacity-100">✕</button>
            </div>
            <div className="bg-black/40 p-4 rounded-xl font-mono text-xs text-emerald-400 overflow-x-auto space-y-1">
              <div>{`{`}</div>
              <div className="pl-4">{`"transaction_id": "${selectedTxn.id}",`}</div>
              <div className="pl-4">{`"tenant_id": "${selectedTxn.tenant}",`}</div>
              <div className="pl-4">{`"utility_account": "${selectedTxn.account}",`}</div>
              <div className="pl-4">{`"amount_usd": ${selectedTxn.amount.replace('$', '')},`}</div>
              <div className="pl-4">{`"status": "${selectedTxn.status}",`}</div>
              <div className="pl-4">{`"idempotency_hash": "${selectedTxn.idempotencyKey}",`}</div>
              <div className="pl-4">{`"owasp_sanitized": true,`}</div>
              <div className="pl-4">{`"pci_tokenized": true`}</div>
              <div>{`}`}</div>
            </div>
            <button 
              onClick={() => setSelectedTxn(null)}
              className="w-full bg-black/20 hover:bg-black/30 border border-white/10 font-bold py-2 rounded-xl text-xs transition-all"
            >
              Close Verification Inspector
            </button>
          </div>
        </div>
      )}

      {/* Helper Component for Key Icon */}
      {/* ================= GLOBAL FOOTER ================= */}
      <footer className={`border-t ${currentTheme.border} ${currentTheme.cardBg} px-4 lg:px-8 py-3 flex flex-wrap items-center justify-between text-xs opacity-60 gap-2 mt-auto`}>
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
          <span>GridFlow SaaS Enterprise Platform • OWASP Top 10 Compliant</span>
        </div>
        <div className="font-mono text-[10px]">
          15 Custom Themes • MERN + RediSearch + LangGraph Cluster
        </div>
      </footer>

    </div>
  );
}

function KeyIcon(props) {
  return (
    <svg {...props} xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="m15.5 7.5 2.3 2.3a1 1 0 0 0 1.4 0l2.1-2.1a1 1 0 0 0 0-1.4L19 4.1a1 1 0 0 0-1.4 0l-2.1 2.1a1 1 0 0 0 0 1.4" />
      <circle cx="7.5" cy="15.5" r="5.5" />
    </svg>
  );
}