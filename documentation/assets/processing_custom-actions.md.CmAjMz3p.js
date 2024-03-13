import{_ as s,c as i,o as a,a4 as n}from"./chunks/framework.DclLByTZ.js";const g=JSON.parse('{"title":"Custom Actions","description":"","frontmatter":{"outline":"deep"},"headers":[],"relativePath":"processing/custom-actions.md","filePath":"processing/custom-actions.md","lastUpdated":null}'),t={name:"processing/custom-actions.md"},l=n(`<h1 id="custom-actions" tabindex="-1">Custom Actions <a class="header-anchor" href="#custom-actions" aria-label="Permalink to &quot;Custom Actions&quot;">​</a></h1><p>When you need to apply custom cross cutting code on processing jobs (i.e. <code>validation</code>, <code>caching</code> etc)</p><h2 id="implementing" tabindex="-1">Implementing <a class="header-anchor" href="#implementing" aria-label="Permalink to &quot;Implementing&quot;">​</a></h2><p>each action has a few things you need to todo in order to apply logic while processing jobs.</p><ul><li>1️⃣ - implement the <code>IProcessAction&lt;T&gt;</code> interface, use the <code>&lt;T&gt;</code> to apply the action against any class.</li><li>2️⃣ - dependency injection</li><li>3️⃣ - implement <code>Execute</code>, which takes in 2 objects <ul><li><code>context</code> - this is the job that is being processed any any addition context.</li><li><code>next</code> - is the delegate to invoke the next Action in the pipeline.</li></ul></li></ul><div class="language-csharp vp-adaptive-theme"><button title="Copy Code" class="copy"></button><span class="lang">csharp</span><pre class="shiki shiki-themes github-light github-dark vp-code"><code><span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">using</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> ClientProcessing</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">;</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">using</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> ClientProcessing</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">Middleware</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">;</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">using</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> Dbones</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">Pipes</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">;</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">public</span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;"> class</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt; : </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">IProcessAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt; </span><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">//1️⃣</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">{</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">    readonly</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> ILogger</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt;&gt; </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">_logger</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">;</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">    //2️⃣</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">    public</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">ILogger</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt;&gt; </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">logger</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">)</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    {</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">        _logger </span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">=</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;"> logger;</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    }</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">    //3️⃣</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">    public</span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;"> async</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> Task</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> Execute</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">JobContext</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt; </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">context</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">, </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">Next</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">JobContext</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt;&gt; </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">next</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">)</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    {</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">        _logger.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">LogInformation</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#032F62;--shiki-dark:#9ECBFF;">&quot;{JobType} - Before - jobId {JobId}&quot;</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">,</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">            typeof</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">).FullName,</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">            context.JobId);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">        await</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;"> next</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(context);</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">        _logger.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">LogInformation</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#032F62;--shiki-dark:#9ECBFF;">&quot;{JobType} - After - jobId {JobId}&quot;</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">,</span></span>
<span class="line"><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">            typeof</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">T</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">).FullName,</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">            context.JobId);</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    }</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">}</span></span></code></pre></div><p>Step 3️⃣, allows you to implement logic before and after the execution of the other Actions (including Handler) in the pipeline chain.</p><p>It also allows you to stop the execution the rest of the down stream pipeline items.</p><h2 id="configuring" tabindex="-1">Configuring <a class="header-anchor" href="#configuring" aria-label="Permalink to &quot;Configuring&quot;">​</a></h2><h3 id="ioc-registration" tabindex="-1">IoC registration <a class="header-anchor" href="#ioc-registration" aria-label="Permalink to &quot;IoC registration&quot;">​</a></h3><div class="important custom-block github-alert"><p class="custom-block-title">IMPORTANT</p><p>you are required to register the type directly, not aginst an interface.</p></div><div class="note custom-block github-alert"><p class="custom-block-title">NOTE</p><p>The <code>Scope</code> is up to you, select the correct one.</p></div><p>You require to tell the IoC container of the type, so it can apply dependency injection correctly.</p><div class="language-csharp vp-adaptive-theme"><button title="Copy Code" class="copy"></button><span class="lang">csharp</span><pre class="shiki shiki-themes github-light github-dark vp-code"><code><span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">collection.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">TryAddScoped</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">typeof</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;&gt;));</span></span></code></pre></div><h3 id="inform-the-pipeline-of-this-action" tabindex="-1">Inform the pipeline of this action <a class="header-anchor" href="#inform-the-pipeline-of-this-action" aria-label="Permalink to &quot;Inform the pipeline of this action&quot;">​</a></h3><div class="warning custom-block github-alert"><p class="custom-block-title">WARNING</p><p>This is under review, however the following will work, with minimal refactoring.</p></div><p>We need to inform of <code>Laters</code> in which order to apply your Custom Actions, it only needs to happen at the application start, here is how you can apply this</p><ul><li>1️⃣ - Apply the <code>ConfigureLaters</code> located on the <code>HostBuilder</code></li><li>2️⃣ - Add to the CustomActions list (add your items in order)</li><li>3️⃣ - all the other setup code</li></ul><div class="language-csharp vp-adaptive-theme"><button title="Copy Code" class="copy"></button><span class="lang">csharp</span><pre class="shiki shiki-themes github-light github-dark vp-code"><code><span class="line"></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">//1️⃣</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">builder.WebHost.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">ConfigureLaters</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">((</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">context</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">, </span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">setup</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">) </span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">=&gt;</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">{</span></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">    //2️⃣</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    setup.ClientActions.CustomActions.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">Add</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">typeof</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">(</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">EpicAction</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;&gt;));</span></span>
<span class="line"></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">    //3️⃣</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    setup.Configuration.WorkerEndpoint </span><span style="--shiki-light:#D73A49;--shiki-dark:#F97583;">=</span><span style="--shiki-light:#032F62;--shiki-dark:#9ECBFF;"> &quot;http://localhost:5000/&quot;</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">;</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">    setup.</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">UseStorage</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&lt;</span><span style="--shiki-light:#6F42C1;--shiki-dark:#B392F0;">UseMarten</span><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">&gt;();</span></span>
<span class="line"><span style="--shiki-light:#6A737D;--shiki-dark:#6A737D;">    //....</span></span>
<span class="line"><span style="--shiki-light:#24292E;--shiki-dark:#E1E4E8;">});</span></span></code></pre></div>`,19),e=[l];function h(p,k,o,r,E,d){return a(),i("div",null,e)}const y=s(t,[["render",h]]);export{g as __pageData,y as default};
