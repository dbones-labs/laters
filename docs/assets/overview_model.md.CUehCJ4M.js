import{_ as e,c as o,o as t,a4 as a}from"./chunks/framework.j5x1Bts0.js";const s="/laters/assets/model.D0ihwK2u.png",b=JSON.parse('{"title":"Model","description":"","frontmatter":{"outline":"deep"},"headers":[],"relativePath":"overview/model.md","filePath":"overview/model.md","lastUpdated":null}'),i={name:"overview/model.md"},l=a('<h1 id="model" tabindex="-1">Model <a class="header-anchor" href="#model" aria-label="Permalink to &quot;Model&quot;">​</a></h1><div class="note custom-block github-alert"><p class="custom-block-title">NOTE</p><p>we store all of these types into the datastore.</p></div><p>Laters has 3 main types it uses inorder to do its work</p><p><img src="'+s+'" alt="model"></p><ul><li><code>Job</code> - This is an instace of a single job, which has been queued to be processed</li><li><code>CronJob</code> - this is a re-occouring job, which contains how often to create a new Job instance based on a CRON</li><li><code>Leader</code> - there is only one entry for leasder, and it represents the node which is acting as leader (won the leader election)</li></ul>',5),n=[l];function r(d,c,h,_,p,m){return t(),o("div",null,n)}const f=e(i,[["render",r]]);export{b as __pageData,f as default};
