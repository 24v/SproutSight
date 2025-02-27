<lane orientation="vertical" horizontal-content-alignment="middle" >
    <banner text="SproutSight Pro"  background={@Mods/StardewUI/Sprites/BannerBackground} background-border-thickness="48,0" padding="12" /> 
    <lane>
        <lane layout="150px content"
              margin="0, 16, 0, 0"
              orientation="vertical"
              horizontal-content-alignment="end"
              z-index="2">
            <frame *repeat={AllTabs}
                   layout="125px 64px"
                   margin={Margin}
                   padding="16, 0"
                   horizontal-content-alignment="middle"
                   vertical-content-alignment="middle"
                   background={@Mods/24v.SproutSight/Sprites/MenuTiles:TabButton}
                   focusable="true"
                   click=|^SelectTab(Value)|>
                <label text={Title} />
            </frame>
        </lane>

        <frame *switch={SelectedTab}
               layout="820px 500px"
               margin="0, 16, 0, 0"
               padding="32, 24"
               background={@Mods/StardewUI/Sprites/ControlBorder}>

            <scrollable peeking="128" layout="stretch">
            <lane *case="Today">
                <scrollable peeking="128">
                    <lane>
                        <lane orientation="vertical">
                            <lane>
                                <label text={TotalProceeds}/>
                                <image layout="24px" margin="5,0,0,20" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                            <label *!if={ShippedSomething} margin="0,20,0,0" text="You have not shipped anything today." />
                            <lane *repeat={CurrentItems} tooltip={Name} vertical-content-alignment="middle">
                                <panel *switch={QualityName} layout="64px" vertical-content-alignment="end">
                                    <image layout="stretch" margin="4" sprite={Sprite} />
                                    <lane vertical-content-alignment="end">
                                        <image *case="Silver" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarSilver} />
                                        <image *case="Gold" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarGold} />
                                        <image *case="Iridium" layout="24px" margin="4" sprite={@Mods/24v.SproutSight/Sprites/Cursors:QualityStarIridium} />
                                        <spacer layout="stretch 0px" />
                                        <digits layout="content" scale="3.0" number={StackCount} />
                                    </lane>
                                </panel>
                                <label text={TotalSalePrice} margin="10,0,5,0"/>
                                <image layout="24px" margin="0,0,10,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                                <label text={FormattedSale}/>
                            </lane>
                        </lane>
                        <lane orientation="vertical" margin="120,0,0,0">
                            <lane>
                                <label text="Other Sales"/>
                                <image layout="24px" margin="5,0,0,10" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                            </lane>
                            <lane layout="content 64px" horizontal-content-alignment="end">
                                <label text="Received:" margin="10,20,0,0"/>
                                <label text={TodayGoldIn} margin="10,20,0,0"/>
                                <label text="g" margin="0,20,0,0"/>
                            </lane>
                            <lane layout="content 64px" horizontal-content-alignment="end">
                                <label text="Spent:" margin="10,20,0,0"/>
                                <label text={TodayGoldOut} color="Red" margin="10,20,0,0"/>
                                <label text="g" margin="0,20,0,10"/>
                            </lane>
                        </lane>
                    </lane>
                </scrollable>
            </lane>

            <lane *case="Shipping" *context={TrackedDataAggregator} layout="820px content" orientation="vertical">
                <lane *repeat={ShippedGrid} orientation="vertical" margin="0,0,0,40">
                <!-- Context is a YearEntryElement(int Year, List<SeasonEntryElement<List<DayEntryEntryElement>>> Value) -->
                    <label text="Total Shipped" margin="0,0,0,10"/>
                    <lane *repeat={Value} vertical-content-alignment="end" margin="0,0,0,10"> 
                    <!-- Context is a SeasonEntryElement(Season Season, List<DayEntryElement> Value, string text, ...) -->
                        <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip}>
                            <image *if={IsSpring} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                            <image *if={IsSummer} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                            <image *if={IsFall} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                            <image *if={IsWinter} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                            <label text={Season} margin="5,0,0,0"/>
                        </lane>
                        <image *repeat={Value} tint={Tint} fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                        <lane *if={IsWinter} margin="0,0,18,0" tooltip={^Tooltip}>
                            <label text="Y-"/>
                            <label text={^Year} />
                            <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                        </lane>
                    </lane>
                </lane>
            </lane>  

            <lane *case="CashFlow" *context={TrackedDataAggregator} layout="820px content" orientation="vertical">
                <label text="Daily Cash Flow" margin="0,0,0,10"/>
                <lane *repeat={CashFlowGrid} orientation="vertical" margin="0,0,0,40">
                <!-- Context is a YearEntryElement(int Year, List<SeasonEntryElement<List<InOutEntry>>> Value) -->
                    <lane *repeat={Value} vertical-content-alignment="end" margin="0,0,0,10"> 
                    <!-- Context is a SeasonEntryElement(Season Season, List<InOutEntry> Value, string text, ...) -->
                        <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip}>
                            <image *if={IsSpring} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                            <image *if={IsSummer} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                            <image *if={IsFall} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                            <image *if={IsWinter} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                            <label text={Season} margin="5,0,0,0"/>
                        </lane>
                        <lane *repeat={Value} orientation="vertical" margin="1,0,0,0">
                        <!-- Context is an InOutEntry -->
                            <image tint={InTint} fit="Stretch" layout={InLayout} tooltip={InTooltip} sprite={@Mods/StardewUI/Sprites/White} />
                            <image tint={OutTint} fit="Stretch" layout={OutLayout} tooltip={OutTooltip} sprite={@Mods/StardewUI/Sprites/White} />
                        </lane>
                        <lane *if={IsWinter} margin="0,0,18,0" tooltip={^Tooltip}>
                            <label text="Y-"/>
                            <label text={^Year} />
                            <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                        </lane>
                    </lane>
                </lane>
            </lane>

            <lane *case="Wallet" *context={TrackedDataAggregator}  orientation="vertical" >
                <label text="Daily Gold In Wallet" margin="0,0,0,10"/>
                <lane *repeat={WalletGrid} orientation="vertical" margin="0,0,0,40">
                <!-- Context is a YearEntryElement(int Year, List<SeasonEntryElement<List<DayEntryEntryElement>>> Value) -->
                    <lane *repeat={Value} vertical-content-alignment="end" margin="0,0,0,10"> 
                    <!-- Context is a SeasonEntryElement(Season Season, List<DayEntryElement> Value, string text, ...) -->
                        <lane layout="140px 40px" vertical-content-alignment="end" tooltip={Tooltip} >
                            <image *if={IsSpring} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Spring} />
                            <image *if={IsSummer} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Summer} />
                            <image *if={IsFall} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Fall} />
                            <image *if={IsWinter} layout="24px 16px" margin="0,0,0,5" sprite={@Mods/24v.SproutSight/Sprites/Cursors:Winter} />
                            <label text={Season} margin="5,0,0,0"/>
                        </lane>
                        <image *repeat={Value} tint={Tint} fit="Stretch" margin="1,0,0,0" layout={Layout} tooltip={Tooltip} sprite={@Mods/StardewUI/Sprites/White} />
                        <lane *if={IsWinter} margin="10,0,0,0" tooltip={^Tooltip}>
                            <label text="Y-"/>
                            <label text={^Year} />
                            <image layout="24px" margin="5,1,0,0" sprite={@Mods/24v.SproutSight/Sprites/Cursors:GoldIcon} />
                        </lane>
                    </lane>
                </lane>
            </lane>
            </scrollable>
        </frame>
    </lane>
</lane>
