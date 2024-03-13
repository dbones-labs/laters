import{_ as e,c as a,o as t,a4 as o}from"./chunks/framework.DclLByTZ.js";const s="/assets/overview-schedule._yl6fu9P.png",r="/assets/overview-process.wdv0bc2t.png",c="/assets/arch-overview.B0MaOGvo.png",i="/assets/all-roles.Bm-cB8mX.png",l="/assets/dedicated-roles.CMZFH-2d.png",w=JSON.parse('{"title":"Archiecture Overview","description":"","frontmatter":{"outline":"deep"},"headers":[],"relativePath":"overview/architecture.md","filePath":"overview/architecture.md","lastUpdated":null}'),n={name:"overview/architecture.md"},d=o('<h1 id="archiecture-overview" tabindex="-1">Archiecture Overview <a class="header-anchor" href="#archiecture-overview" aria-label="Permalink to &quot;Archiecture Overview&quot;">​</a></h1><h2 id="concepts" tabindex="-1">Concepts <a class="header-anchor" href="#concepts" aria-label="Permalink to &quot;Concepts&quot;">​</a></h2><p><code>Laters</code> is split into a 2 main flows (+ a couple of others, which we will cover)</p><h3 id="schedule" tabindex="-1">Schedule <a class="header-anchor" href="#schedule" aria-label="Permalink to &quot;Schedule&quot;">​</a></h3><p>While your application processes logic it will queue Jobs to be processed for later. this can be in a form of a fire-and-forget or a Cron, these are ways to queue work to be processed.</p><p><img src="'+s+'" alt="An image"></p><p>All scheduled work, is stored in a datastore, to allow it to be processed by different instances of the same application.</p><h3 id="process" tabindex="-1">Process <a class="header-anchor" href="#process" aria-label="Permalink to &quot;Process&quot;">​</a></h3><p>Processes which are a Leader will scan the Datastore to jobs to be process, and send them to the workers to process, the workers update the job accordingly.</p><p><img src="'+r+'" alt="An image"></p><h2 id="target-architectures" tabindex="-1">Target Architectures <a class="header-anchor" href="#target-architectures" aria-label="Permalink to &quot;Target Architectures&quot;">​</a></h2><div class="note custom-block github-alert"><p class="custom-block-title">NOTE</p><p>you can run this with a <code>single instnace</code> and no loadbalance (by targeting localhost)</p></div><p>Although <code>Laters</code> is a .NET package, it takes <code>advantage</code> of a common architecture style, many services behind a <code>loadbalancer</code> (Service in Kubernetes).</p><p><img src="'+c+'" alt="An image"></p><p>so we can apply <code>Laters</code> in a few different ways</p><ul><li>all services, can be all roles</li><li>dedicated leaders and workers</li></ul><h3 id="all-roles" tabindex="-1">All Roles <a class="header-anchor" href="#all-roles" aria-label="Permalink to &quot;All Roles&quot;">​</a></h3><div class="note custom-block github-alert"><p class="custom-block-title">NOTE</p><p>Any node can be elected leader</p></div><p>This solution is designed to be simpler, where one instance will be elected leader, and all instances are workers</p><p><img src="'+i+'" alt="An image"></p><ul><li>blue - Leader</li><li>green - Worker</li></ul><h3 id="dedicated-roles" tabindex="-1">Dedicated Roles <a class="header-anchor" href="#dedicated-roles" aria-label="Permalink to &quot;Dedicated Roles&quot;">​</a></h3><div class="note custom-block github-alert"><p class="custom-block-title">NOTE</p><p>The leader does not process jobs (at all)</p></div><p>This solutions is designed for niche situations (massive performance), where one instance is designated as the leader (cordoned off from the loadbalancer), and the worker only concentate on processing</p><p><img src="'+l+'" alt="An image"></p><ul><li>blue - Leader</li><li>green - Worker</li></ul>',26),h=[d];function p(u,m,b,g,v,_){return t(),a("div",null,h)}const k=e(n,[["render",p]]);export{w as __pageData,k as default};
