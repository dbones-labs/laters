import{_ as e,c as i,o as a,a4 as o}from"./chunks/framework.j5x1Bts0.js";const n="/laters/assets/client-pipeline-overview.Dx3bKqFZ.png",_=JSON.parse('{"title":"Client Pipeline","description":"","frontmatter":{"outline":"deep"},"headers":[],"relativePath":"processing/client-pipeline.md","filePath":"processing/client-pipeline.md","lastUpdated":null}'),t={name:"processing/client-pipeline.md"},l=o('<h1 id="client-pipeline" tabindex="-1">Client Pipeline <a class="header-anchor" href="#client-pipeline" aria-label="Permalink to &quot;Client Pipeline&quot;">​</a></h1><div class="important custom-block github-alert"><p class="custom-block-title">IMPORTANT</p><p>Performance is faster on 2nd exection and onwards. See <a href="./client-pipeline.html#performance">Performance</a></p></div><p>When processing a job, we run the job through <code>middleware</code> which you can extend.</p><p>This allows us to apply a number of actions before and after the Handler exexutues, which means you can add custome logic as you see fit (i.e. <code>caching</code>, <code>validation</code> etc)</p><h2 id="pipeline-overview" tabindex="-1">pipeline overview <a class="header-anchor" href="#pipeline-overview" aria-label="Permalink to &quot;pipeline overview&quot;">​</a></h2><p>The pipeline looks like this:</p><p><img src="'+n+'" alt="An image"></p><p>we have the following 3 area&#39;s</p><ul><li><code>Laters Actions</code> - this is where we apply logic which processes the current job.</li><li><code>Custom Actions</code> - any actions your appliciation would apply.</li><li><code>Handler</code> - the particular logic to be applied against the single job type.</li></ul><p>This pipeline is very similar to ones which you will find in MVC, MassTransit etc.</p><h2 id="performance" tabindex="-1">Performance <a class="header-anchor" href="#performance" aria-label="Permalink to &quot;Performance&quot;">​</a></h2><p>On first run for each Job Type, a pipeline is compiled, which means 1st run will be slower, and all following exections will run alot faster.</p><p>The is done this way to allow each Job Type to have unique actions if required.</p>',13),r=[l];function c(s,p,d,h,u,m){return a(),i("div",null,r)}const w=e(t,[["render",c]]);export{_ as __pageData,w as default};
